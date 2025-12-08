FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 7777

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["StreamKey.Hub/StreamKey.Hub.csproj", "StreamKey.Hub/"]
COPY ["StreamKey.Core/StreamKey.Core.csproj", "StreamKey.Core/"]
COPY ["StreamKey.Infrastructure/StreamKey.Infrastructure.csproj", "StreamKey.Infrastructure/"]
COPY ["StreamKey.Shared/StreamKey.Shared.csproj", "StreamKey.Shared/"]

RUN dotnet restore "StreamKey.Hub/StreamKey.Hub.csproj"

COPY . .
WORKDIR "/src/StreamKey.Hub"
RUN dotnet build "./StreamKey.Hub.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StreamKey.Hub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StreamKey.Hub.dll"]
