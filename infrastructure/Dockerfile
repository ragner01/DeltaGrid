# Base Dockerfile template for DeltaGrid services
# Copy this to each service directory and customize as needed

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/*/*.csproj", "./"]
RUN for file in $(ls *.csproj); do mkdir -p "src/${file%.csproj}" && mv "$file" "src/${file%.csproj}/"; done

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build application
WORKDIR /src/src/WebApi  # Change to appropriate service directory
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]  # Change to appropriate service DLL


