#!/bin/bash

# Script to build and pack the NuGet package

echo "=== Building WorkflowEngine.Core NuGet Package ==="
echo ""

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean WorkflowEngine.Core/WorkflowEngine.Core.csproj --configuration Release
rm -rf WorkflowEngine.Core/bin/Release/*.nupkg

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore WorkflowEngine.Core/WorkflowEngine.Core.csproj

# Build the project
echo "Building project..."
dotnet build WorkflowEngine.Core/WorkflowEngine.Core.csproj --configuration Release

# Run tests (if any)
# echo "Running tests..."
# dotnet test

# Pack the NuGet package
echo "Creating NuGet package..."
dotnet pack WorkflowEngine.Core/WorkflowEngine.Core.csproj --configuration Release --output ./nupkg

echo ""
echo "✓ NuGet package created successfully!"
echo "Package location: ./nupkg/"
echo ""
echo "To publish to NuGet.org:"
echo "  dotnet nuget push ./nupkg/WorkflowEngine.Core.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
echo ""
echo "To publish to a local feed:"
echo "  dotnet nuget push ./nupkg/WorkflowEngine.Core.*.nupkg --source /path/to/local/feed"
