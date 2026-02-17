# 1. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# wait-for-it ishlashi uchun bash kerak
USER root
RUN apt-get update && apt-get install -y bash curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# ✅ Skriptni to'g'ridan-to'g'ri GitHubdan ildiz katalogga yuklab olamiz
ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /wait-for-it.sh
USER root
RUN chmod +x /wait-for-it.sh

# Loyiha fayllari
COPY ["src/backend/Forex.WebApi/Forex.WebApi.csproj", "src/backend/Forex.WebApi/"]
COPY ["src/backend/Forex.Application/Forex.Application.csproj", "src/backend/Forex.Application/"]
COPY ["src/backend/Forex.Domain/Forex.Domain.csproj", "src/backend/Forex.Domain/"]
COPY ["src/backend/Forex.Infrastructure/Forex.Infrastructure.csproj", "src/backend/Forex.Infrastructure/"]

RUN dotnet restore "src/backend/Forex.WebApi/Forex.WebApi.csproj"
# Cache bust: 2026-02-17-16:00-final-fix
COPY . .
WORKDIR "/src/src/backend/Forex.WebApi"
RUN dotnet build "Forex.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/backend/Forex.WebApi"
RUN dotnet publish "Forex.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. Final runtime stage
FROM base AS final
WORKDIR /app

# ✅ Skriptni ildiz katalogga nusxalaymiz (/wait-for-it.sh)
COPY --from=build /wait-for-it.sh /wait-for-it.sh
COPY --from=publish /app/publish .

# ✅ Copy appsettings.Production.json from source
COPY --from=build /src/src/backend/Forex.WebApi/appsettings.Production.json ./appsettings.Production.json

ENTRYPOINT ["dotnet", "Forex.WebApi.dll"]