@echo off
rem NET.runner-92 Phase 11 benchmark: runs the game's view benchmark and saves
rem the results in a "benchmark" folder next to the game. Put this file next to
rem NETRunner92_Hospital.exe and double-click it.
rem Takes about 15 minutes. Do not touch the mouse or keyboard while it runs.
rem Runs 1 to 3 measure Low, Medium and High. Runs 4 to 8 measure Low with one
rem feature switched off (-net92-without) or another upscaler (-net92-upscaler),
rem and runs 9 and 10 do the same on Medium, so their frame times show what each
rem costs on this computer (#36, round 3).
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
call :run 4 "Low without distant lights" Low "views-low-without-distant-lights" "-net92-without distant-lights"
call :run 5 "Low without the fill lights" Low "views-low-without-fill-lights" "-net92-without fill-lights"
call :run 6 "Low without glass" Low "views-low-without-glass" "-net92-without glass"
call :run 7 "Low without rain, dust and steam" Low "views-low-without-particles" "-net92-without particles"
call :run 8 "Low with the FSR 1 upscaler" Low "views-low-upscaler-fsr1" "-net92-upscaler fsr1"
call :run 9 "Medium without the lights in the fog" Medium "views-medium-without-fog-lights" "-net92-without fog-lights"
call :run 10 "Medium without shadows" Medium "views-medium-without-shadows" "-net92-without shadows"

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
echo [%~1/10] %~2 (about a minute and a half)...
start "" /wait "%here%NETRunner92_Hospital.exe" -net92-benchmark -net92-quality %~3 %~5 -net92-profile "%here%benchmark\profile-%~4" -net92-output "%here%benchmark\%~4.csv"
rd /s /q "%here%benchmark\profile-%~4" 2>nul
exit /b 0
