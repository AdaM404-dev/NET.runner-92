@echo off
rem NET.runner-92 benchmark: runs the game's built-in performance captures
rem and saves the results in a "benchmark" folder next to the game.
rem Put this file next to NET.runner-92.exe and double-click it.
rem Takes about 6 minutes. Do not touch the mouse or keyboard while it runs.

cd /d "%~dp0"
if not exist "NET.runner-92.exe" (
  echo NET.runner-92.exe was not found. Put this file next to the game and try again.
  pause
  exit /b 1
)
if not exist "benchmark" mkdir "benchmark"

echo [1/4] Five fixed views on Low (about 40 seconds)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-output "%~dp0benchmark\views-low.csv"
echo [2/4] Five fixed views on Medium (about 40 seconds)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Medium -net92-output "%~dp0benchmark\views-medium.csv"
echo [3/4] Five fixed views on High (about 40 seconds)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality High -net92-output "%~dp0benchmark\views-high.csv"
echo [4/4] Walking the whole route on Medium (about 3 minutes)...
start "" /wait "NET.runner-92.exe" -net92-route-benchmark -net92-quality Medium -net92-output "%~dp0benchmark\route-medium"

rem Only the hardware names, nothing that identifies the computer or its user.
powershell -NoProfile -Command "$c=(Get-CimInstance Win32_Processor).Name; $g=Get-CimInstance Win32_VideoController | ForEach-Object { $_.Name + ' (driver ' + $_.DriverVersion + ')' }; $m=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB); 'CPU: ' + $c; 'GPU: ' + ($g -join '; '); 'RAM: ' + $m + ' GB'" > "%~dp0benchmark\system.txt"

echo.
echo Done. Please zip the "benchmark" folder and attach it to a GitHub issue,
echo or send it to Samuel.
explorer "%~dp0benchmark"
pause
