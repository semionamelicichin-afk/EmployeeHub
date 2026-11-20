@echo off
setlocal
powershell.exe -NoLogo -NoProfile -Command "$running = Get-Process -Name 'EmployeeHub' -ErrorAction SilentlyContinue; if ($running) { Write-Host 'Close EmployeeHub before resetting its data.'; exit 2 }"
if errorlevel 1 (
  echo Reset did not complete.
  exit /b 1
)
set "RESET_CONFIRM="
set /p "RESET_CONFIRM=WARNING: this deletes all local EmployeeHub database changes. Type RESET to continue: "
powershell.exe -NoLogo -NoProfile -Command "$ErrorActionPreference = 'Stop'; if ($env:RESET_CONFIRM -cne 'RESET') { Write-Host 'Reset cancelled.'; exit 1 }; $running = Get-Process -Name 'EmployeeHub' -ErrorAction SilentlyContinue; if ($running) { Write-Host 'Close EmployeeHub before resetting its data.'; exit 2 }; $directory = Join-Path $env:LOCALAPPDATA 'EmployeeHub'; $database = Join-Path $directory 'EmployeeHub.db'; foreach ($path in @($database, ($database + '-wal'), ($database + '-shm'), ($database + '-journal'))) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force } }; Write-Host 'EmployeeHub data was reset. The demo data will be recreated on next launch.'"
if errorlevel 1 (
  echo Reset did not complete.
  exit /b 1
)
echo Reset complete.
