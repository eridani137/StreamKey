FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5555

RUN apt-get update && \
    apt-get install -y --no-install-recommends tzdata && \
    ln -fs /usr/share/zoneinfo/Europe/Moscow /etc/localtime && \
    dpkg-reconfigure -f noninteractive tzdata && \
    apt-get install -y --no-install-recommends \
        libgssapi-krb5-2 \
        curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["StreamKey.Api/StreamKey.Api.csproj", "StreamKey.Api/"]
COPY ["StreamKey.Core/StreamKey.Core.csproj", "StreamKey.Core/"]
COPY ["StreamKey.Infrastructure/StreamKey.Infrastructure.csproj", "StreamKey.Infrastructure/"]
COPY ["StreamKey.Shared/StreamKey.Shared.csproj", "StreamKey.Shared/"]
COPY ["StreamKey.Hub/StreamKey.Hub.csproj", "StreamKey.Hub/"]

RUN dotnet restore "StreamKey.Api/StreamKey.Api.csproj"

COPY . .
WORKDIR "/src/StreamKey.Api"
RUN dotnet build "./StreamKey.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StreamKey.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StreamKey.Api.dll"]