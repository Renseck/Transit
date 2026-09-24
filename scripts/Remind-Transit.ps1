#Requires -Version 5.1
<#
.SYNOPSIS
  Desktop reminder: if today's work location is not explicitly logged in Transit,
  show a Windows toast with one button per location (one-click logging via the
  transit: URI handler) plus Open. Separately, while last month has no
  declaration run, show a "declare" toast that opens /declaration on that month.
  Runs in the interactive user session (IIS Session 0 cannot raise toasts).
.PARAMETER Force
  Skip the weekend guard and the already-logged check and show the attendance
  toast unconditionally (manual verification). Buttons still come from the app
  when it is reachable. The declare toast still only shows when a month is due.
.NOTES
  Registered as a Scheduled Task by Install-Transit.ps1. Copied to
  C:\ProgramData\Transit so it does not depend on the repo location.
#>
[CmdletBinding()]
param([switch]$Force)

$StatusUrl = 'http://transit.local:8083/api/reminder/today-status'
$OpenUrl   = 'http://transit.local:8083'
$Dash      = [char]0x2014

function Get-NetworkNames {
    try {
        @(Get-NetConnectionProfile -ErrorAction Stop | ForEach-Object { $_.Name } | Where-Object { $_ })
    } catch {
        @()
    }
}

function Get-TodayStatus {
    $query = (Get-NetworkNames | ForEach-Object { 'network=' + [uri]::EscapeDataString($_) }) -join '&'
    $url = if ($query) { "$StatusUrl`?$query" } else { $StatusUrl }
    try {
        Invoke-RestMethod -Uri $url -TimeoutSec 10
    } catch {
        # Fail open: if we cannot confirm today is logged, remind anyway (Open-only toast).
        Write-Verbose "Status check failed: $($_.Exception.Message). Reminding anyway."
        $null
    }
}

# Weekday rule lives here so the endpoint and task triggers stay simple.
if (-not $Force -and (Get-Date).DayOfWeek -in 'Saturday', 'Sunday') { return }

$status = Get-TodayStatus

if (-not (Get-Module -ListAvailable -Name BurntToast)) {
    Write-Warning 'BurntToast module not installed; cannot show reminder. Re-run Install-Transit.ps1.'
    return
}

Import-Module BurntToast

# Declare toast: independent of today's attendance. The fixed identifier makes the midday
# run replace the logon toast in the Action Center instead of stacking a second one.
if ($status -and $status.pendingDeclaration) {
    $pending = $status.pendingDeclaration
    $declare = New-BTButton -Content 'Declare' -Arguments "$OpenUrl/declaration?month=$($pending.query)" -ActivationType Protocol
    $later   = New-BTButton -Dismiss -Content 'Later'
    New-BurntToastNotification -Text 'Transit', "$($pending.label) isn't declared yet $Dash review, sign and send it." `
        -Button $declare, $later -UniqueIdentifier 'transit-declare'
}

if (-not $Force -and $status -and $status.explicitlyLogged) { return }   # already logged -> silent

$buttons = @()
if ($status) {
    # [datetime] normalises the date whether ConvertFrom-Json kept it a string (5.1) or parsed it (7+).
    $date = ([datetime]$status.date).ToString('yyyy-MM-dd')
    foreach ($option in @($status.options)) {
        $uri = "transit:log?location=$($option.locationId)&date=$date"
        $buttons += New-BTButton -Content $option.name -Arguments $uri -ActivationType Protocol
    }
}
$buttons += New-BTButton -Content 'Open' -Arguments $OpenUrl -ActivationType Protocol

$body = if ($status -and $status.suggestion) {
    "You're on $($status.suggestion.network) $Dash log today as $($status.suggestion.name)?"
} else {
    "You haven't logged today's work location yet."
}

New-BurntToastNotification -Text 'Transit', $body -Button $buttons
