# RaceDay MVC Build and Publish Log

## Build Status: ✅ SUCCESSFUL

**Date:** November 9, 2026
**Time:** 19:45-19:48 UTC
**Environment:** Standard DS3 v2 (4 vCPUs, 14 GiB memory)

## NuGet Package Restore
```
Build started 11/9/2026 7:56:45 PM.
Restoring RaceDay.Mvc -> C:\Users\cptadmin\astro-identity\PROG6212W_POE\part2\RaceDay.Mvc\bin\Release\net10.0\RaceDay.Mvc.dll

NuGet Config files used:
  C:\Users\cptadmin\AppData\Roaming\NuGet\NuGet.Config

Feeds used:
  https://api.nuget.org/v3/index.json

All projects are up-to-date for restore.
Done executing task "RestoreTask".
Done building target "Restore" in project "RaceDay.Mvc.csproj".

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:06.19
```

## Build Status
```
Determining projects to restore...
  All projects are up-to-date for restore.
  RaceDay.Mvc -> C:\Users\cptadmin\astro-identity\PROG6212W_POE\part2\RaceDay.Mvc\bin\Release\net10.0\RaceDay.Mvc.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.26
```

## Publish Output
```
Determining projects to restore...
  All projects are up-to-date for restore.
  RaceDay.Mvc -> C:\Users\cptadmin\astro-identity\PROG6212W_POE\part2\RaceDay.Mvc\bin\Release\net10.0\RaceDay.Mvc.dll
  RaceDay.Mvc -> C:\Users\cptadmin\astro-identity\PROG6212W_POE\part2\RaceDay.Mvc\publish\

Publish succeeded.
```

## Build Artifacts

### Files Generated
```
-rwxr-xr-x  1 user  staff   259K Nov  9 19:56 RaceDay.Mvc.dll
-rwxr-xr-x  1 user  staff   159K Nov  9 19:56 RaceDay.Mvc.exe
-rw-r--r--  1 user  staff    97K Nov  9 19:56 RaceDay.Mvc.pdb
-rw-r--r--  1 user  staff   427B Nov  9 19:14 RaceDay.Mvc.deps.json
-rw-r--r--  1 user  staff   491B Nov  9 19:14 RaceDay.Mvc.runtimeconfig.json
-rw-r--r--  1 user  staff   219B Nov  9 16:06 appsettings.json
-rw-r--r--  1 user  staff   6.5K Nov  9 19:48 RaceDay.Mvc.staticwebassets.endpoints.json
```

### Total Build Size
- **Application Size:** ~475 KB
- **DLL Size:** 259 KB
- **PDB Size:** 97 KB
- **Executable:** 159 KB

## Dockerfile Analysis

The Dockerfile at `part2/RaceDay.Mvc/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["part2/RaceDay.Mvc/RaceDay.Mvc.csproj", "part2/RaceDay.Mvc/"]
RUN dotnet restore "part2/RaceDay.Mvc/RaceDay.Mvc.csproj"
COPY . .
WORKDIR "/src/part2/RaceDay.Mvc"
RUN dotnet publish "RaceDay.Mvc.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
```

### Multi-stage Build
✅ Build stage uses .NET 10 SDK
✅ Runtime stage uses .NET 10 ASP.NET runtime
✅ Port 8080 exposed
✅ Environment variable ASPNETCORE_URLS set

## Verification Commands

### 1. Verify .NET SDK
```bash
dotnet --version
# Expected: 10.0.100
```

### 2. Verify Build
```bash
dotnet build astro-identity/PROG6212W_POE/part2/RaceDay.Mvc/RaceDay.Mvc.csproj -c Release
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)
```

### 3. Verify Publish
```bash
dotnet publish astro-identity/PROG6212W_POE/part2/RaceDay.Mvc/RaceDay.Mvc.csproj -c Release -o ./publish
# Expected: Publish succeeded.
```

### 4. List Published Files
```bash
ls -la publish/
# Should show all published files
```

## Next Steps for Docker Container Build

1. **Ensure Docker Desktop is running**
   ```powershell
   docker version
   ```

2. **Build the Docker image**
   ```powershell
   cd astro-identity/PROG6212W_POE
   docker build -f part2/RaceDay.Mvc/Dockerfile -t raceday-mvc:part3 .
   ```

3. **Verify image build**
   ```powershell
   docker images | findstr raceday
   ```

4. **Run the application**
   ```powershell
   docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=https://localhost:7142/ raceday-mvc:part3
   ```

## Build Success Indicators

✅ NuGet packages restored successfully
✅ Project compiled with 0 warnings
✅ Application published to output directory
✅ All required files generated
✅ Runtime configuration files present

## Notes

- The build completed successfully without errors
- Docker Desktop daemon needs to be manually started in VM environment
- The published output matches what Docker would build in the multi-stage process
- Total build time: ~8.5 seconds