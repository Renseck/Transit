#Requires -Version 5.1
<#
.SYNOPSIS
  Desktop reminder: if today's work location is not explicitly logged in Transit,
  show a Windows toast that deep-links to the app. Runs in the interactive user
  session (IIS Session 0 cannot raise toasts).
.PARAMETER Force
  Skip the weekend guard and status check and show the toast unconditionally
  (manual verification).
.NOTES
  Registered as a Scheduled Task by Install-Transit.ps1. Copied to
  C:\ProgramData\Transit so it does not depend on the repo location.
#>
[CmdletBinding()]
param([switch]$Force)

$StatusUrl = 'http://transit.local:8083/api/reminder/today-status'
$OpenUrl   = 'http://transit.local:8083'

if (-not $Force) {
    # Weekday rule lives here so the endpoint and task triggers stay simple.
    if ((Get-Date).DayOfWeek -in 'Saturday', 'Sunday') { return }
    try {
        $status = Invoke-RestMethod -Uri $StatusUrl -TimeoutSec 10
        if ($status.explicitlyLogged) { return }   # already logged -> silent
    } catch {
        # Fail open: if we cannot confirm today is logged, remind anyway.
        Write-Verbose "Status check failed: $($_.Exception.Message). Reminding anyway."
    }
}

if (-not (Get-Module -ListAvailable -Name BurntToast)) {
    Write-Warning 'BurntToast module not installed; cannot show reminder. Re-run Install-Transit.ps1.'
    return
}

Import-Module BurntToast
$button = New-BTButton -Content 'Log now' -Arguments $OpenUrl -ActivationType Protocol
New-BurntToastNotification `
    -Text 'Transit', "You haven't logged today's work location yet." `
    -Button $button