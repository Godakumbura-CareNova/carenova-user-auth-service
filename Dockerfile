# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project file
COPY ["src/UserAuth.Api/UserAuth.Api.csproj", "src/UserAuth.Api/"]

# Restore dependencies
RUN dotnet restore "src/UserAuth.Api/UserAuth.Api.csproj"

# Copy source code
COPY . .

# Publish application
RUN dotnet publish "src/UserAuth.Api/UserAuth.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "UserAuth.Api.dll"]