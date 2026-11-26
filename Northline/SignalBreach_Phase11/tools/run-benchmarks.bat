@echo off
rem NET.runner-92 Phase 11 benchmark: runs the game's view benchmark and saves
rem the results in a "benchmark" folder next to the game. Put this file next to
rem NETRunner92_Hospital.exe and double-click it.
rem Takes about 10 minutes. Do not touch the mouse or keyboard while it runs,
rem and keep other programs (Task Manager too) closed: they share the GPU.
rem Runs 1 to 3 measure Low, Medium and High. Runs 4 to 6 switch one feature
rem off (-net92-without), so their frame times show what it costs on this
rem computer (#36, round 3). Each run ends with the paused game's frame.
rem Each run starts from the menu, loads the hospital itself and skips the
rem studio intro; a fresh profile folder per run keeps saved settings out.

cd /d "%~dp0"
rem Inside the :run subroutine %~dp0 no longer names this folder, so keep it here.
set "here=%~dp0"
if not exist "NETRunner92_Hospital.exe" (
  echo NETRunner92_Hospital.exe was not found. Put this file next to the game and try again.
  pause
  exit /b 1
)
if exist "benchmark" rd /s /q "benchmark"
mkdir "benchmark"

call :run 1 "Five fixed views on Low" Low "views-low" ""
call :run 2 "Five fixed views on Medium" Medium "views-medium" ""
call :run 3 "Five fixed views on High" High "views-high" ""
call :run 4 "Low without glass" Low "views-low-without-glass" "-net92-without glass"
call :run 5 "Medium without shadows" Medium "views-medium-without-shadows" "-net92-without shadows"
call :run 6 "Low without the fill lights" Low "views-low-without-fill-lights" "-net92-without fill-lights"

rem Only the hardware names, nothing that identifies the computer or its user.
powershell -NoProfile -Command "$c=(Get-CimInstance Win32_Processor).Name; $g=Get-CimInstance Win32_VideoController | ForEach-Object { $_.Name + ' (driver ' + $_.DriverVersion + ')' }; $m=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB); 'CPU: ' + $c; 'GPU: ' + ($g -join '; '); 'RAM: ' + $m + ' GB'" > "%~dp0benchmark\system.txt"

echo.
echo Done. Please zip the "benchmark" folder and attach it to a GitHub issue,
echo or send it to Samuel.
explorer "%~dp0benchmark"
pause
exit /b 0

rem One run: number, what it measures, quality level, result file name, extra options.
rem The profile folder holds only the settings that run used, so it is removed afterwards.
:run
echo [%~1/6] %~2 (about a minute and a half)...
start "" /wait "%here%NETRunner92_Hospital.exe" -net92-benchmark -net92-quality %~3 %~5 -net92-profile "%here%benchmark\profile-%~4" -net92-output "%here%benchmark\%~4.csv"
rd /s /q "%here%benchmark\profile-%~4" 2>nul
exit /b 0
