#!/bin/bash
# RaceDay MVC Verification Script
# This script verifies the build and prepares for Docker deployment

echo "========================================="
echo "RaceDay MVC Build Verification Script"
echo "========================================="
echo ""

# Set .NET environment
export DOTNET_ROOT=/c/Users/cptadmin/AppData/Local/dotnet
export PATH=$DOTNET_ROOT:$PATH

# Navigate to project
cd astro-identity/PROG6212W_POE/part2/RaceDay.Mvc

echo "Step 1: Verifying .NET SDK..."
dotnet --version
if [ $? -eq 0 ]; then
    echo "✓ .NET SDK verified"
else
    echo "✗ .NET SDK not found"
    exit 1
fi
echo ""

echo "Step 2: Restoring NuGet packages..."
dotnet restore
if [ $? -eq 0 ]; then
    echo "✓ Packages restored"
else
    echo "✗ Package restore failed"
    exit 1
fi
echo ""

echo "Step 3: Building project..."
dotnet build -c Release
if [ $? -eq 0 ]; then
    echo "✓ Build successful"
else
    echo "✗ Build failed"
    exit 1
fi
echo ""

echo "Step 4: Publishing output..."
dotnet publish -c Release -o ./publish
if [ $? -eq 0 ]; then
    echo "✓ Publish successful"
else
    echo "✗ Publish failed"
    exit 1
fi
echo ""

echo "Step 5: Verifying publish output..."
if [ -f "./publish/RaceDay.Mvc.dll" ]; then
    echo "✓ RaceDay.Mvc.dll found"
else
    echo "✗ RaceDay.Mvc.dll not found"
    exit 1
fi

if [ -f "./publish/appsettings.json" ]; then
    echo "✓ appsettings.json found"
else
    echo "✗ appsettings.json not found"
    exit 1
fi
echo ""

echo "Step 6: Checking Dockerfile..."
cd ../..
if [ -f "part2/RaceDay.Mvc/Dockerfile" ]; then
    echo "✓ Dockerfile found"
    echo ""
    echo "Dockerfile content:"
    echo "-----------------"
    cat part2/RaceDay.Mvc/Dockerfile
else
    echo "✗ Dockerfile not found"
    exit 1
fi
echo ""

echo "========================================="
echo "Build Verification Complete!"
echo "========================================="
echo ""
echo "Next steps for Docker:"
echo "1. Ensure Docker Desktop is running"
echo "2. Run: docker build -f part2/RaceDay.Mvc/Dockerfile -t raceday-mvc:part3 ."
echo "3. Run: docker run -p 8080:8080 -e ApiSettings__BaseUrl=https://localhost:7142/ raceday-mvc:part3"
echo ""