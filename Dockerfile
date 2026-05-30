FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY simple-ws-dotnet.csproj .
COPY Models/ Models/
COPY Services/ Services/
COPY WebSocket/ WebSocket/
COPY Properties/ Properties/
COPY appsettings.json .
COPY appsettings.Development.json .
COPY Program.cs .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "simple-ws-dotnet.dll"]
