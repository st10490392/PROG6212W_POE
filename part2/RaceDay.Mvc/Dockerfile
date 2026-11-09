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
