param(
    [ValidateSet('Enable','Disable')][string]$Action,
    [string]$Executable
)
$ErrorActionPreference = 'Stop'
$taskName = 'PlayStationControllerTools-LoLInput'
$taskIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not ([Security.Principal.WindowsPrincipal]::new($taskIdentity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this setup script in an administrator PowerShell. The ordinary tray/rumble app stays non-elevated.'
}
if (-not $Executable) { $Executable = Join-Path $PSScriptRoot '..\dist\LoLDamageRumble.exe' }
$Executable = [IO.Path]::GetFullPath($Executable)
if (-not (Test-Path -LiteralPath $Executable -PathType Leaf) -or [IO.Path]::GetFileName($Executable) -ne 'LoLDamageRumble.exe') {
    throw 'Select your reviewed, locally built LoLDamageRumble.exe.'
}
$taskDirectory = Split-Path -Parent $Executable
if ($Action -eq 'Disable') {
    $taskStatus = Join-Path $taskDirectory 'input-status.json'
    if (Test-Path -LiteralPath $taskStatus) {
        $taskState = Get-Content -LiteralPath $taskStatus -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($taskState.running -and (Get-Process -Id $taskState.processId -ErrorAction SilentlyContinue)) {
            throw 'First uncheck Space as right Ctrl in the LoL tray, and wait for the worker to exit. Do not force-stop a modifier owner.'
        }
    }
    Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue | Unregister-ScheduledTask -Confirm:$false
    return
}
$taskPrincipal = New-ScheduledTaskPrincipal -UserId $taskIdentity.Name -LogonType Interactive -RunLevel Highest
$taskAction = New-ScheduledTaskAction -Execute $Executable -Argument '--input-worker' -WorkingDirectory $taskDirectory
$taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew
$taskDefinition = New-ScheduledTask -Action $taskAction -Principal $taskPrincipal -Settings $taskSettings -Description 'Optional foreground LoL Space-to-right-Ctrl worker. Same exe, no HID, no independent tray/login trigger.'
Register-ScheduledTask -TaskName $taskName -InputObject $taskDefinition -Force | Out-Null
