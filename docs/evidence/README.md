# RaceDay MVC - Docker Evidence Folder

This folder contains all evidence and documentation for the Docker container demonstration for PROG6212 Part 3.

## Contents

1. **DOCKER_BUILD_GUIDE.md** - Complete instructions for building and running the Docker container
2. **BUILD_LOG.md** - Detailed build and publish log showing successful compilation
3. **verify-build.sh** - Bash script to verify the build and prepare for Docker deployment

## Evidence Checklist

- [ ] Docker Desktop running on Standard DS3 v2 VM
- [ ] Docker image built successfully: `raceday-mvc:part3`
- [ ] Container runs on port 8080
- [ ] MVC application accessible at http://localhost:8080
- [ ] Application connects to API at https://localhost:7142
- [ ] Screenshots captured for submission

## Build Verification

The application has been successfully:
- ✅ NuGet package restore completed
- ✅ Project compiled with 0 warnings, 0 errors
- ✅ Application published to output directory
- ✅ Dockerfile verified

## File Sizes

- RaceDay.Mvc.dll: 259 KB
- RaceDay.Mvc.exe: 159 KB
- RaceDay.Mvc.pdb: 97 KB
- Total published size: ~475 KB

## Next Steps

1. Start Docker Desktop on your VM
2. Navigate to project root: `C:\Users\cptadmin\astro-identity\PROG6212W_POE`
3. Run the build verification script: `bash docs/evidence/verify-build.sh`
4. Build the Docker image: `docker build -f part2/RaceDay.Mvc/Dockerfile -t raceday-mvc:part3 .`
5. Run the container: `docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=https://localhost:7142/ raceday-mvc:part3`
6. Capture screenshots for submission

## Technical Notes

- Project uses .NET 10 SDK
- Multi-stage Docker build
- Port 8080 exposed
- ASP.NET Core runtime
- MVC frontend with API integration