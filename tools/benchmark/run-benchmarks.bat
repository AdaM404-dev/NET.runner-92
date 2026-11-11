@echo off
rem NET.runner-92 benchmark: runs the game's built-in performance captures
rem and saves the results in a "benchmark" folder next to the game.
rem Put this file next to NET.runner-92.exe and double-click it.
rem Takes about 15 minutes. Do not touch the mouse or keyboard while it runs.
rem Runs 4 to 11 switch one feature off each (-net92-without), so their
rem frame times show what that feature costs on this computer.

cd /d "%~dp0"
if not exist "NET.runner-92.exe" (
  echo NET.runner-92.exe was not found. Put this file next to the game and try again.
  pause
  exit /b 1
)
if not exist "benchmark" mkdir "benchmark"

echo [1/12] Five fixed views on Low (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-output "%~dp0benchmark\views-low.csv"
echo [2/12] Five fixed views on Medium (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Medium -net92-output "%~dp0benchmark\views-medium.csv"
echo [3/12] Five fixed views on High (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality High -net92-output "%~dp0benchmark\views-high.csv"
echo [4/12] Five fixed views on Low without occlusion-culling (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without occlusion-culling -net92-output "%~dp0benchmark\views-low-without-occlusion-culling.csv"
echo [5/12] Five fixed views on Low without volumetric-fog (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without volumetric-fog -net92-output "%~dp0benchmark\views-low-without-volumetric-fog.csv"
echo [6/12] Five fixed views on Low without fog-lights (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without fog-lights -net92-output "%~dp0benchmark\views-low-without-fog-lights.csv"
echo [7/12] Five fixed views on Low without shadows (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without shadows -net92-output "%~dp0benchmark\views-low-without-shadows.csv"
echo [8/12] Five fixed views on Low without ambient-occlusion (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without ambient-occlusion -net92-output "%~dp0benchmark\views-low-without-ambient-occlusion.csv"
echo [9/12] Five fixed views on Low without distant-lights (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without distant-lights -net92-output "%~dp0benchmark\views-low-without-distant-lights.csv"
echo [10/12] Five fixed views on Low without transparent-objects (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without transparent-objects -net92-output "%~dp0benchmark\views-low-without-transparent-objects.csv"
echo [11/12] Five fixed views on Low without decals (about a minute)...
start "" /wait "NET.runner-92.exe" -net92-benchmark -net92-quality Low -net92-without decals -net92-output "%~dp0benchmark\views-low-without-decals.csv"
echo [12/12] Walking the whole route on Low (about 4 minutes)...
start "" /wait "NET.runner-92.exe" -net92-route-benchmark -net92-quality Low -net92-output "%~dp0benchmark\route-low"

rem Only the hardware names, nothing that identifies the computer or its user.
powershell -NoProfile -Command "$c=(Get-CimInstance Win32_Processor).Name; $g=Get-CimInstance Win32_VideoController | ForEach-Object { $_.Name + ' (driver ' + $_.DriverVersion + ')' }; $m=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB); 'CPU: ' + $c; 'GPU: ' + ($g -join '; '); 'RAM: ' + $m + ' GB'" > "%~dp0benchmark\system.txt"

echo.
echo Done. Please zip the "benchmark" folder and attach it to a GitHub issue,
echo or send it to Samuel.
explorer "%~dp0benchmark"
pause
