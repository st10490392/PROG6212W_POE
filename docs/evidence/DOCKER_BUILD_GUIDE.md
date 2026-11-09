# Docker Build and Deployment Guide for RaceDay MVC

## Overview
This document provides instructions for building and running the RaceDay MVC container on your Standard DS3 v2 VM (4 vCPUs, 14 GiB memory).

## Prerequisites
- Standard DS3 v2 VM (4 vCPUs, 14 GiB memory) ✅ **Meets requirements**
- Docker Desktop installed and running
- WSL2 enabled (recommended)
- Hyper-V enabled (if using Windows containers)

## Docker Desktop Setup Steps

### 1. Enable WSL2 (Recommended)
```powershell
# Check WSL version
wsl --version

# If version 1, upgrade to WSL2
wsl --set-version <DistroName> 2

# Set WSL2 as default
wsl --set-default-version 2
```

### 2. Start Docker Desktop
```powershell
# Start Docker Desktop service
Start-Service com.docker.service

# Open Docker Desktop GUI (should auto-start)
Start-Process "C:\Program Files\Docker\Docker Desktop.exe"
```

### 3. Verify Docker is Running
```powershell
# Wait 10-15 seconds for Docker to initialize
Timeout /t 15

# Check Docker version
docker version
```

## Build Instructions

### Build the Docker Image
```powershell
# Navigate to project root
cd "C:\Users\cptadmin\astro-identity\PROG6212W_POE"

# Build the Docker image
docker build -f part2/RaceDay.Mvc/Dockerfile -t racday-mvc:part3 .
```

### Alternative: Build with Buildx (Multi-platform support)
```powershell
docker buildx build --platform linux/amd64,linux/arm64 \
  -f part2/RaceDay.Mvc/Dockerfile \
  -t racday-mvc:part3 \
  --load .
```

## Run Instructions

### 1. Start the API First
The MVC frontend requires the RaceDay API to be running.

```powershell
# Start the API (from part2 directory)
export DOTNET_ROOT=/c/Users/cptadmin/AppData/Local/dotnet
export PATH=$DOTNET_ROOT:$PATH
dotnet run --project RaceDay.Api/RaceDay.Api.csproj --launch-profile https
```

### 2. Run the MVC Container
```powershell
docker run --rm \
  --name raceday-mvc \
  -p 8080:8080 \
  -e ApiSettings__BaseUrl=https://localhost:7142/ \
  racday-mvc:part3
```

## Troubleshooting

### Docker Daemon Not Starting
If Docker Desktop shows "Docker daemon not running":

1. **Restart Docker Services**
```powershell
Restart-Service com.docker.service -Force
Stop-Service com.docker.service -Force
Start-Service com.docker.service -Force
```

2. **Check Docker Desktop Logs**
```powershell
# Navigate to Docker Desktop logs
Get-Content "$env:APPDATA\Docker\log\docker-desktop.log" -Tail 50
```

3. **Reset to Factory Defaults**
   - Open Docker Desktop
   - Click whale menu > Troubleshoot > Reset to factory defaults

### Common Issues

**Issue: Docker daemon not responding**
- **Solution**: Restart Docker Desktop and wait 2-3 minutes for initialization
- **VM Requirement**: Ensure Hyper-V is enabled in VM settings

**Issue: API connection refused**
- **Solution**: Verify API is running on https://localhost:7142 before starting MVC container
- **Solution**: Update appsettings.json BaseUrl if needed

**Issue: Port 8080 already in use**
- **Solution**: Use different port: `-p 8081:8080`

## Build Verification

### Check Build Output
```powershell
# List built images
docker images | findstr racday

# Inspect image
docker inspect racday-mvc:part3
```

### Test Container
```powershell
# Run container interactively
docker run --rm -it \
  -e ApiSettings__BaseUrl=https://localhost:7142/ \
  racday-mvc:part3 powershell

# Inside container, test application
ls -la /app
# Should show all published files
```

## Expected Output

When successful, you should see:
1. Docker image `raceday-mvc:part3` built without errors
2. Container running on `http://localhost:8080`
3. MVC application accessible and connecting to API

## Evidence Collection

After successful build and run, capture screenshots for submission:

1. **Docker build output** showing successful image creation
2. **Running container** with `docker ps`
3. **Application URL** `http://localhost:8080` in browser
4. **Docker Desktop dashboard** showing running container

## Build Success Criteria

✅ Docker Desktop services running
✅ Image built successfully
✅ Container starts without errors
✅ MVC frontend accessible on http://localhost:8080
✅ Connects to API at https://localhost:7142

## Notes for Standard DS3 v2 VM

Your VM configuration (Standard DS3 v2 with 4 vCPUs and 14 GiB memory) meets the requirements for:
- Running Docker Desktop with WSL2 backend
- Building multi-stage Docker images
- Running both API and MVC containers simultaneously

**Recommended VM Settings**:
- Ensure at least 8 GB RAM allocated
- Enable nested virtualization if using containers
- SSD storage for faster image builds