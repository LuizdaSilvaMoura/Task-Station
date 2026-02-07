FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/TaskStation.Domain/TaskStation.Domain.csproj TaskStation.Domain/
COPY src/TaskStation.Application/TaskStation.Application.csproj TaskStation.Application/
COPY src/TaskStation.Infrastructure/TaskStation.Infrastructure.csproj TaskStation.Infrastructure/
COPY src/TaskStation.API/TaskStation.API.csproj TaskStation.API/
RUN dotnet restore TaskStation.API/TaskStation.API.csproj

# Copy everything else and build
COPY src/ .
RUN dotnet publish TaskStation.API/TaskStation.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "TaskStation.API.dll"]
