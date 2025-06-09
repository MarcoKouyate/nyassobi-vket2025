@echo off
set UNITY_EXE=%~1
set PROJECT_PATH=%~2
set UNITY_PID=%3

echo UNITY_EXE: [%UNITY_EXE%]
echo PROJECT_PATH: [%PROJECT_PATH%]
echo UNITY_PID: [%UNITY_PID%]

:waitloop
tasklist /FI "PID eq %UNITY_PID%" | findstr /I "Unity.exe" >nul
if not errorlevel 1 (
    timeout /T 1 >nul
    goto waitloop
)

start "" "%UNITY_EXE%" -projectPath "%PROJECT_PATH%"