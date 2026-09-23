#Requires -Version 5.1
<#
.SYNOPSIS
  transit: URI handler for one-click logging from the reminder toast.
  Parses transit:log?location=<id>&date=<yyyy-MM-dd>, POSTs to the loopback
  log-today endpoint, and shows a result toast. Anything malformed is ignored.
.PARAMETER Uri
  The full transit: URI, passed by Windows as "%1".
.NOTES
  Registered under HKCU\Software\Classes\transit by Install-Transit.ps1 and
  copied to C:\ProgramData\Transit. Keep this file ASCII (Windows PowerShell 5.1).
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Uri)

$LogUrl  = 'http://transit.local:8083/api/reminder/log-today'
$OpenUrl = 'http://transit.local:8083'

function Show-Result {
    param([string]$Message, [switch]$WithOpen)

    Import-Module BurntToast -ErrorAction SilentlyContinue
    if (-not (Get-Command New-BurntToastNotification -ErrorAction SilentlyContinue)) { return }

    if (-not $WithOpen) {
        New-BurntToastNotification -Text 'Transit', $Message
        return
    }

    $open = New-BTButton -Content 'Open' -Arguments $OpenUrl -ActivationType Protocol
    New-BurntToastNotification -Text 'Transit', $Message -Button $open
}

# Body of a failed request as text, or $null. PowerShell 7 fills ErrorDetails; Windows PowerShell 5.1
# leaves it empty for non-2xx JSON responses, so fall back to reading the response stream.
function Get-ErrorBody {
    param($ErrorRecord)

    if ($ErrorRecord.ErrorDetails -and $ErrorRecord.ErrorDetails.Message) { return $ErrorRecord.ErrorDetails.Message }

    $response = $ErrorRecord.Exception.Response
    if (-not $response -or -not ($response -is [System.Net.WebResponse])) { return $null }

    try {
        $stream = $response.GetResponseStream()
        if ($stream.CanSeek) { $stream.Position = 0 }
        return (New-Object System.IO.StreamReader($stream)).ReadToEnd()
    } catch {
        return $null
    }
}

# Strict parse; tolerate the "//" and trailing "/" some launchers add.
if ($Uri -notmatch '^transit:(//)?log/?\?location=(\d+)&date=(\d{4}-\d{2}-\d{2})/?$') { return }
$locationId = [int]$Matches[2]
$dateText   = $Matches[3]

try {
    $date = [datetime]::ParseExact($dateText, 'yyyy-MM-dd', [cultureinfo]::InvariantCulture)
} catch {
    return
}
$dayLabel = $date.ToString('ddd d MMM')

$body = @{ date = $dateText; locationId = $locationId } | ConvertTo-Json
$detail = $null

try {
    $result = Invoke-RestMethod -Uri $LogUrl -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 10
    Show-Result "Logged $($result.locationName) for $dayLabel $([char]0x2713)"
    return
} catch {
    # 409/404 carry a JSON body; unreachable/timeout have none.
    $errorBody = Get-ErrorBody $_
    if ($errorBody) {
        try { $detail = $errorBody | ConvertFrom-Json } catch { $detail = $null }
    }
}

if ($detail -and $detail.outcome -eq 'AlreadyLogged') {
    Show-Result "Today is already logged as $($detail.locationName)." -WithOpen
    return
}

if ($detail -and $detail.outcome -eq 'StaleReminder') {
    Show-Result "That reminder was for $dayLabel. Open Transit to edit it." -WithOpen
    return
}

Show-Result "Couldn't log $([char]0x2014) open Transit to log today." -WithOpen
