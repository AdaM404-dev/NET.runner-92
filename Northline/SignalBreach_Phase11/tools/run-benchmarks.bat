@echo off
rem NET.runner-92 Phase 11 benchmark: runs the game's view benchmark on Low,
rem Medium and High and saves the results in a "benchmark" folder next to the
rem game. Put this file next to NETRunner92_Hospital.exe and double-click it.
rem Takes about 5 minutes. Do not touch the mouse or keyboard while it runs.
rem Each run starts from the menu, loads the hospital itself and skips the
rem studio intro; a fresh profile folder per run keeps saved settings out.

cd /d "%~dp0"
if not exist "NETRunner92_Hospital.exe" (
  echo NETRunner92_Hospital.exe was not found. Put this file next to the game and try again.
  pause
  exit /b 1
)
if exist "benchmark" rd /s /q "benchmark"
mkdir "benchmark"

echo [1/3] Five fixed views on Low (about a minute and a half)...
start "" /wait "NETRunner92_Hospital.exe" -net92-benchmark -net92-quality Low -net92-profile "%~dp0benchmark\profile-low" -net92-output "%~dp0benchmark\views-low.csv"
echo [2/3] Five fixed views on Medium (about a minute and a half)...
start "" /wait "NETRunner92_Hospital.exe" -net92-benchmark -net92-quality Medium -net92-profile "%~dp0benchmark\profile-medium" -net92-output "%~dp0benchmark\views-medium.csv"
echo [3/3] Five fixed views on High (about a minute and a half)...
start "" /wait "NETRunner92_Hospital.exe" -net92-benchmark -net92-quality High -net92-profile "%~dp0benchmark\profile-high" -net92-output "%~dp0benchmark\views-high.csv"

rem Only the hardware names, nothing that identifies the computer or its user.
powershell -NoProfile -Command "$c=(Get-CimInstance Win32_Processor).Name; $g=Get-CimInstance Win32_VideoController | ForEach-Object { $_.Name + ' (driver ' + $_.DriverVersion + ')' }; $m=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB); 'CPU: ' + $c; 'GPU: ' + ($g -join '; '); 'RAM: ' + $m + ' GB'" > "%~dp0benchmark\system.txt"

rem The profile folders hold only the settings each run used; they are not needed.
rd /s /q "benchmark\profile-low" "benchmark\profile-medium" "benchmark\profile-high" 2>nul

echo.
echo Done. Please zip the "benchmark" folder and attach it to a GitHub issue,
echo or send it to Samuel.
explorer "%~dp0benchmark"
pause
