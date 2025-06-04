@echo off
echo Uninstalling DueTime Tracking Service...

REM Check for admin privileges
NET SESSION >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo This script requires administrator privileges.
    echo Please right-click on the script and select "Run as administrator".
    pause
    exit /b 1
)

REM Stop the service if it's running
echo Stopping service...
sc stop DueTimeTracker >nul 2>&1

REM Wait a moment for the service to stop
timeout /t 2 >nul

REM Uninstall the service
echo Removing service...
sc delete DueTimeTracker
if %ERRORLEVEL% neq 0 (
    echo Failed to remove the service.
    pause
    exit /b 1
)

echo.
echo DueTime Tracking Service has been uninstalled successfully.
echo.

pause 