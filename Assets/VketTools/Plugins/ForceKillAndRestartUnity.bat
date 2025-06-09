@echo off
set UNITY_EXE=%~1
set PROJECT_PATH=%~2
set UNITY_PID=%3

echo UNITY_EXE: [%UNITY_EXE%]
echo PROJECT_PATH: [%PROJECT_PATH%]
echo UNITY_PID: [%UNITY_PID%]

taskkill /PID %UNITY_PID% /F
:: 少し待つ
timeout /T 2 >nul

start "" "%UNITY_EXE%" -projectPath "%PROJECT_PATH%"