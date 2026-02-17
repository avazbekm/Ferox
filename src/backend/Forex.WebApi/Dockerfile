FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y bash curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /wait-for-it.sh
USER root
RUN chmod +x /wait-for-it.sh

COPY ["src/backend/Forex.WebApi/Forex.WebApi.csproj", "src/backend/Forex.WebApi/"]
COPY ["src/backend/Forex.Application/Forex.Application.csproj", "src/backend/Forex.Application/"]
COPY ["src/backend/Forex.Domain/Forex.Domain.csproj", "src/backend/Forex.Domain/"]
COPY ["src/backend/Forex.Infrastructure/Forex.Infrastructure.csproj", "src/backend/Forex.Infrastructure/"]

RUN dotnet restore "src/backend/Forex.WebApi/Forex.WebApi.csproj"
COPY . .
WORKDIR "/src/src/backend/Forex.WebApi"
RUN dotnet build "Forex.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/backend/Forex.WebApi"
RUN dotnet publish "Forex.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /wait-for-it.sh /wait-for-it.sh
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Forex.WebApi.dll"]