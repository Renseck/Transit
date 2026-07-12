#Requires -Version 5.1
<#
.SYNOPSIS
  Provisions a bare Windows machine to host Transit under IIS, and redeploys the app.
  Idempotent: safe to re-run as the "update to latest repo" command.
.NOTES
  Run from inside the cloned repo. Self-elevates to Administrator.
  Targets Windows PowerShell 5.1 so it works before pwsh is installed.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# --- Self-elevate ---------------------------------------------------------
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$identity
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host '[Transit] Elevation required - relaunching as Administrator...'
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`""
    )
    exit
}

# --- Constants ------------------------------------------------------------
$RepoRoot   = Split-Path -Parent $PSScriptRoot            # scripts\ -> repo root
$WebProj    = Join-Path $RepoRoot 'src\Transit.Web\Transit.Web.csproj'
$TestProj   = Join-Path $RepoRoot 'tests\Transit.Tests\Transit.Tests.csproj'
$PublishDir = 'C:\inetpub\wwwroot\Transit'
$DataRoot   = 'C:\ProgramData\Transit'
$AppPool    = 'Transit'
$SiteName   = 'transit'
$Port       = 83
$PoolIdent  = "IIS AppPool\$AppPool"
$SdkUrl     = 'https://aka.ms/dotnet/10.0/dotnet-sdk-win-x64.exe'      # latest 10.0.x SDK
$HostingUrl = 'https://aka.ms/dotnet/10.0/dotnet-hosting-win.exe'      # latest 10.0.x ASP.NET Core Hosting Bundle
$AncmDll    = Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'
$SofficeExe = Join-Path $env:ProgramFiles 'LibreOffice\program\soffice.exe'

# --- Logging / summary ----------------------------------------------------
$script:Summary   = New-Object System.Collections.Generic.List[string]
$script:RebootFlag = $false

function Write-Phase   { param([string]$Message) Write-Host "`n[Transit] == $Message ==" -ForegroundColor Cyan }
function Write-Info    { param([string]$Message) Write-Host "[Transit]    $Message" }
function Add-Summary   { param([string]$Message) $script:Summary.Add($Message) | Out-Null; Write-Info $Message }

function Invoke-Preflight {
    Write-Phase 'Preflight'
    if (-not (Test-Path $WebProj)) {
        throw "Web project not found at '$WebProj'. Run this script from inside the cloned repo (scripts\Install-Transit.ps1)."
    }
    Write-Info "Found web project: $WebProj"

    $conflict = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($conflict) {
        $owningPids = ($conflict.OwningProcess | Sort-Object -Unique) -join ', '
        Write-Info "Note: port $Port already has a listener (PID(s): $owningPids). If that is a previous Transit deploy this is fine; otherwise free the port."
    } else {
        Write-Info "Port $Port is free."
    }
}

function Install-IisFeatures {
    Write-Phase 'IIS Windows features'
    $features = @('IIS-WebServerRole', 'IIS-WebServer', 'IIS-ManagementScriptingTools')
    foreach ($f in $features) {
        $state = (Get-WindowsOptionalFeature -Online -FeatureName $f).State
        if ($state -eq 'Enabled') {
            Write-Info "$f already enabled."
            continue
        }
        Write-Info "Enabling $f ..."
        $result = Enable-WindowsOptionalFeature -Online -FeatureName $f -All -NoRestart
        if ($result.RestartNeeded) { $script:RebootFlag = $true }
        Add-Summary "Enabled IIS feature: $f"
    }
}

function Invoke-SilentInstaller {
    param([string]$Url, [string]$FileName, [string[]]$Arguments)
    $dest = Join-Path $env:TEMP $FileName
    Write-Info "Downloading $FileName ..."
    Invoke-WebRequest -Uri $Url -OutFile $dest -UseBasicParsing
    Write-Info "Running $FileName ..."
    $proc = Start-Process -FilePath $dest -ArgumentList $Arguments -Wait -PassThru
    Remove-Item $dest -ErrorAction SilentlyContinue
    if ($proc.ExitCode -eq 3010) {
        $script:RebootFlag = $true
        Write-Info "$FileName installed; a reboot is pending (exit 3010)."
        return
    }
    if ($proc.ExitCode -ne 0) {
        throw "$FileName failed with exit code $($proc.ExitCode)."
    }
}

function Test-DotnetSdk10 {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { return $false }
    $sdks = & dotnet --list-sdks 2>$null
    return [bool]($sdks | Where-Object { $_ -match '^10\.' })
}

function Install-DotnetSdk {
    Write-Phase '.NET 10 SDK'
    if (Test-DotnetSdk10) {
        Write-Info '.NET 10 SDK already present.'
        return
    }
    Invoke-SilentInstaller -Url $SdkUrl -FileName 'dotnet-sdk-10-win-x64.exe' -Arguments @('/install', '/quiet', '/norestart')
    Add-Summary 'Installed .NET 10 SDK'
    # Refresh PATH for this session so later dotnet calls resolve.
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}

function Install-HostingBundle {
    Write-Phase 'ASP.NET Core Hosting Bundle'
    if (Test-Path $AncmDll) {
        Write-Info 'Hosting Bundle (ANCM v2) already present.'
        return
    }
    Invoke-SilentInstaller -Url $HostingUrl -FileName 'dotnet-hosting-10-win.exe' -Arguments @('/install', '/quiet', '/norestart')
    Add-Summary 'Installed ASP.NET Core Hosting Bundle'
    # The bundle registers the IIS module; bounce WAS/W3SVC so IIS loads it.
    Write-Info 'Restarting IIS (net stop was / net start w3svc) to load the module...'
    & net stop was /y | Out-Null
    & net start w3svc | Out-Null
}

function Install-LibreOffice {
    Write-Phase 'LibreOffice'
    if (Test-Path $SofficeExe) {
        Write-Info 'LibreOffice already present.'
        return
    }
    if (-not (Get-Command choco -ErrorAction SilentlyContinue)) {
        throw "Chocolatey not found and LibreOffice is missing. Install Chocolatey (https://chocolatey.org/install) or LibreOffice manually, then re-run."
    }
    Write-Info 'Installing LibreOffice via Chocolatey...'
    & choco install libreoffice-still -y
    if ($LASTEXITCODE -ne 0) { throw "choco install libreoffice-still failed (exit $LASTEXITCODE)." }
    Add-Summary 'Installed LibreOffice (libreoffice-still)'
}

function New-TransitAppPool {
    Write-Phase "App pool '$AppPool'"
    if (-not (Test-Path "IIS:\AppPools\$AppPool")) {
        New-WebAppPool -Name $AppPool | Out-Null
        Add-Summary "Created app pool '$AppPool'"
    } else {
        Write-Info "App pool '$AppPool' already exists."
    }
    # No Managed Code: empty managedRuntimeVersion.
    Set-ItemProperty "IIS:\AppPools\$AppPool" -Name managedRuntimeVersion -Value ''
    if ((Get-WebAppPoolState -Name $AppPool).Value -ne 'Started') {
        Start-WebAppPool -Name $AppPool
    }
    Write-Info "App pool '$AppPool' set to No Managed Code and started."
}

function Set-TransitFolders {
    Write-Phase 'Folders + ACL'
    foreach ($dir in @($DataRoot, $PublishDir)) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Add-Summary "Created folder: $dir"
        } else {
            Write-Info "Folder exists: $dir"
        }
    }
    # Grant the app-pool identity Modify (object + container inherit) on the data root.
    Write-Info "Granting Modify on $DataRoot to '$PoolIdent'..."
    & icacls $DataRoot /grant "${PoolIdent}:(OI)(CI)M" /T | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "icacls grant on $DataRoot failed (exit $LASTEXITCODE)." }
    Add-Summary "Granted '$PoolIdent' Modify on $DataRoot"
}

# --- Main -----------------------------------------------------------------
Write-Phase 'Transit installer starting'
Write-Info  "Repo root:   $RepoRoot"
Write-Info  "Publish dir: $PublishDir"
Write-Info  "Data root:   $DataRoot"
Invoke-Preflight
Install-IisFeatures
Install-DotnetSdk
Install-HostingBundle
Install-LibreOffice
Import-Module WebAdministration
New-TransitAppPool
Set-TransitFolders