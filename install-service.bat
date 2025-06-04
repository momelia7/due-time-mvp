@echo off
echo Installing DueTime Tracking Service...

REM Check for admin privileges
NET SESSION >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo This script requires administrator privileges.
    echo Please right-click on the script and select "Run as administrator".
    pause
    exit /b 1
)

REM Stop the service if it's already running
sc stop DueTimeTracker >nul 2>&1

REM Uninstall the service if it already exists
sc delete DueTimeTracker >nul 2>&1

REM Get the current directory
set CURRENT_DIR=%~dp0

REM Build the service in release mode
echo Building service...
dotnet publish -c Release -o "%CURRENT_DIR%\publish" DueTime.Service\DueTime.Service.csproj
if %ERRORLEVEL% neq 0 (
    echo Failed to build the service.
    pause
    exit /b 1
)

REM Install the service
echo Installing service...
sc create DueTimeTracker binPath= "%CURRENT_DIR%\publish\DueTime.Service.exe" start= auto DisplayName= "DueTime Time Tracking Service"
if %ERRORLEVEL% neq 0 (
    echo Failed to create the service.
    pause
    exit /b 1
)

REM Set the service description
sc description DueTimeTracker "Records time entries based on active windows and user activity"

REM Start the service
echo Starting service...
sc start DueTimeTracker
if %ERRORLEVEL% neq 0 (
    echo Failed to start the service.
    pause
    exit /b 1
)

echo.
echo DueTime Tracking Service has been installed and started successfully.
echo You can manage the service from the DueTime application or from Windows Services.
echo.

pause 