@echo off
REM Script to build and pack the NuGet package

echo === Building WorkflowEngine.Core NuGet Package ===
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean WorkflowEngine.Core\WorkflowEngine.Core.csproj --configuration Release
del /Q WorkflowEngine.Core\bin\Release\*.nupkg 2>nul

REM Restore dependencies
echo Restoring dependencies...
dotnet restore WorkflowEngine.Core\WorkflowEngine.Core.csproj

REM Build the project
echo Building project...
dotnet build WorkflowEngine.Core\WorkflowEngine.Core.csproj --configuration Release

REM Run tests (if any)
REM echo Running tests...
REM dotnet test

REM Pack the NuGet package
echo Creating NuGet package...
if not exist "nupkg" mkdir nupkg
dotnet pack WorkflowEngine.Core\WorkflowEngine.Core.csproj --configuration Release --output .\nupkg

echo.
echo Package created successfully!
echo Package location: .\nupkg\
echo.
echo To publish to NuGet.org:
echo   dotnet nuget push .\nupkg\WorkflowEngine.Core.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
echo.
echo To publish to a local feed:
echo   dotnet nuget push .\nupkg\WorkflowEngine.Core.*.nupkg --source C:\path\to\local\feed
echo.
pause
