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

# --- Main -----------------------------------------------------------------
Write-Phase 'Transit installer starting'
Write-Info  "Repo root:   $RepoRoot"
Write-Info  "Publish dir: $PublishDir"
Write-Info  "Data root:   $DataRoot"