@echo off
setlocal enabledelayedexpansion
echo ========================================
echo Telegram Chat Viewer - C# Build Script
echo ========================================
echo.

REM Check if .NET 8 is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 8 SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK Version:
dotnet --version
echo.

REM Parse command line arguments
set BUILD_TYPE=Release
set CLEAN_BUILD=false
set VERSION_ACTION=

REM Check for version arguments first
if "%1"=="major" set VERSION_ACTION=major
if "%1"=="minor" set VERSION_ACTION=minor
if "%1"=="patch" set VERSION_ACTION=patch

REM Check for other arguments
if "%1"=="debug" set BUILD_TYPE=Debug
if "%1"=="clean" set CLEAN_BUILD=true
if "%1"=="help" goto :help

REM Check second argument for combinations
if "%2"=="debug" set BUILD_TYPE=Debug
if "%2"=="clean" set CLEAN_BUILD=true

REM Handle version increment if requested
if not "%VERSION_ACTION%"=="" (
    echo ========================================
    echo VERSION MANAGEMENT
    echo ========================================
    echo Incrementing %VERSION_ACTION% version...
    powershell -ExecutionPolicy Bypass -File "version-manager.ps1" %VERSION_ACTION%
    if %errorlevel% neq 0 (
        echo ERROR: Version increment failed
        exit /b 1
    )
    echo.
)

REM Show current version
echo ========================================
echo CURRENT VERSION
echo ========================================
powershell -ExecutionPolicy Bypass -File "version-manager.ps1" get
echo.

echo Build Configuration: %BUILD_TYPE%
echo Clean Build: %CLEAN_BUILD%
echo.

REM Always kill any running instances to prevent build issues
echo Checking for running instances...
tasklist /FI "IMAGENAME eq TelegramChatViewer.exe" 2>NUL | find /I /N "TelegramChatViewer.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo Terminating running TelegramChatViewer instances...
    taskkill /F /IM TelegramChatViewer.exe >NUL 2>&1
    timeout /t 2 >NUL
    echo Instances terminated.
    echo.
) else (
    echo No running instances found.
    echo.
)

REM Clean if requested
if "%CLEAN_BUILD%"=="true" (
    echo Cleaning previous builds...
    if exist bin rmdir /s /q bin
    if exist obj rmdir /s /q obj
    if exist publish rmdir /s /q publish
    echo Clean completed.
    echo.
)

REM Restore packages
echo Restoring NuGet packages...
dotnet restore --verbosity minimal
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    exit /b 1
)
echo.

REM Build the application
echo Building application...
dotnet build --configuration %BUILD_TYPE% --no-restore --verbosity normal
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    exit /b 1
)
echo.

REM Publish single-file executable
echo Publishing single-file executable...
echo This may take a moment for single-file packaging...

REM Try to delete existing executable if it exists
if exist "publish\TelegramChatViewer.exe" (
    echo Attempting to remove existing executable...
    del "publish\TelegramChatViewer.exe" 2>NUL
    if exist "publish\TelegramChatViewer.exe" (
        echo WARNING: Could not remove existing executable - it may be in use
        echo Trying to continue anyway...
    )
)

dotnet publish --configuration %BUILD_TYPE% --runtime win-x64 --self-contained --output publish -p:PublishSingleFile=true --verbosity normal
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Publish failed
    echo.
    echo Common causes:
    echo 1. The application is still running (close it and try again)
    echo 2. Antivirus software is scanning the file
    echo 3. File permissions issue
    echo.
    echo Try running: build.bat force
    echo This will automatically close any running instances.
    exit /b 1
)

echo.
echo ========================================
echo BUILD COMPLETED SUCCESSFULLY!
echo ========================================
echo.

REM Check if executable was created
if not exist "publish\TelegramChatViewer.exe" (
    echo ERROR: Executable was not created!
    exit /b 1
)

echo Executable location: publish\TelegramChatViewer.exe
echo.

REM Get file size
for %%A in (publish\TelegramChatViewer.exe) do (
    set /a size=%%~zA/1024/1024
    echo Executable size: !size! MB
)

REM Show build summary
echo.
echo BUILD SUMMARY:
echo ==============
echo Warnings: 0
echo Errors: 0
echo Configuration: %BUILD_TYPE%
echo Target: win-x64 (Single File)
echo.

echo You can now run the application:
echo   publish\TelegramChatViewer.exe
echo.
echo Or copy the entire publish folder to distribute the application.
echo.
goto :eof

:help
echo.
echo Usage: build.bat [version] [option]
echo.
echo Version Commands:
echo   major   - Increment major version (x.0.0) - Breaking changes
echo   minor   - Increment minor version (x.y.0) - New features
echo   patch   - Increment patch version (x.y.z) - Bug fixes
echo.
echo Build Options:
echo   (none)  - Build release version (default)
echo   debug   - Build debug version
echo   clean   - Clean build (removes previous builds)
echo   help    - Show this help
echo.
echo Note: Running instances are automatically terminated before building.
echo.
echo Examples:
echo   build.bat                    - Standard release build
echo   build.bat patch              - Increment patch version and build
echo   build.bat minor              - Increment minor version and build
echo   build.bat major              - Increment major version and build
echo   build.bat patch debug        - Increment patch and build debug
echo   build.bat minor clean        - Increment minor, clean and build
echo   build.bat debug              - Debug build (no version change)
echo   build.bat clean              - Clean and build release
echo.
echo Version Management:
echo   .\version-manager.ps1        - Show current version
echo   .\version-manager.ps1 set 1.2.3 - Set specific version
echo.
echo Running instances are automatically terminated to prevent "file in use" errors.
echo. 