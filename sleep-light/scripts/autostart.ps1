param(
    [ValidateSet('Enable','Disable')][string]$Action,
    [string]$Executable
)
$ErrorActionPreference = 'Stop'
if (-not $Action) { throw 'Specify -Action Enable or -Action Disable.' }
$startupFolder = [Environment]::GetFolderPath('Startup')
if (-not $startupFolder) { throw 'Current-user Startup folder is unavailable.' }
$shortcutPath = Join-Path $startupFolder 'ControllerLight.lnk'
$legacyShortcutPath = Join-Path $startupFolder 'DS4SleepLight.lnk'
if ($Action -eq 'Disable') {
    if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath }
    if (Test-Path -LiteralPath $legacyShortcutPath) { Remove-Item -LiteralPath $legacyShortcutPath }
    Write-Output 'Login autostart disabled. Stop the running watcher separately with ControllerLight.exe --stop.'
    return
}
if (-not $Executable) { $Executable = Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\ControllerLight.exe' }
$resolvedExecutable = (Resolve-Path -LiteralPath $Executable).ProviderPath
if ([IO.Path]::GetFileName($resolvedExecutable) -notin @('ControllerLight.exe','DS4SleepLight.exe')) { throw 'Expected a locally built ControllerLight.exe (or its DS4SleepLight.exe alias).' }
New-Item -ItemType Directory -Path $startupFolder -Force | Out-Null
$shell = New-Object -ComObject WScript.Shell
try {
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $resolvedExecutable
    $shortcut.WorkingDirectory = Split-Path -Parent $resolvedExecutable
    $shortcut.Description = 'PS4 / PS5 sleep and idle light watcher'
    $shortcut.IconLocation = $resolvedExecutable + ',0'
    $shortcut.WindowStyle = 7
    $shortcut.Save()
    # Write the replacement before retiring the old login entry.
    if (Test-Path -LiteralPath $legacyShortcutPath) { Remove-Item -LiteralPath $legacyShortcutPath }
} finally { if ($shortcut) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut) }; [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) }
Write-Output "Login autostart enabled: $shortcutPath"
Write-Output 'This script did not start the watcher. Run your locally built executable for the current session.'
