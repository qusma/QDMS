# QDMS server (src/QDMS.Host). Build from the repo root:
#   docker build -t qdms .
# See deploy/docker-compose.yml for running it with Postgres.

# web UI (src/QDMS.Web); vite writes it to ../QDMS.Host/wwwroot/ui
FROM node:22-alpine AS web
WORKDIR /src/src/QDMS.Web
COPY src/QDMS.Web/package.json src/QDMS.Web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/QDMS.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# restore first so the package layer is cached when only code changes
COPY Directory.Build.props Directory.Packages.props ./
COPY src/QDMS.Common/QDMS.Common.csproj src/QDMS.Common/
COPY src/QDMSClient/QDMSClient.csproj src/QDMSClient/
COPY src/QDMS.Dal/QDMS.Dal.csproj src/QDMS.Dal/
COPY src/QDMS.Server/QDMS.Server.csproj src/QDMS.Server/
COPY src/QDMS.Host/QDMS.Host.csproj src/QDMS.Host/
COPY src/QDMS.DataStorage/QDMS.Server.DataStorage.Postgres/QDMS.Server.DataStorage.Postgres.csproj src/QDMS.DataStorage/QDMS.Server.DataStorage.Postgres/
COPY src/QDMS.DataSources/QDMS.Server.DataSources.Binance/QDMS.Server.DataSources.Binance.csproj src/QDMS.DataSources/QDMS.Server.DataSources.Binance/
COPY src/QDMS.DataSources/QDMS.Server.DataSources.FRED/QDMS.Server.DataSources.FRED.csproj src/QDMS.DataSources/QDMS.Server.DataSources.FRED/
COPY src/QDMS.DataSources/QDMS.Server.DataSources.InteractiveBrokers/QDMS.Server.DataSources.InteractiveBrokers.csproj src/QDMS.DataSources/QDMS.Server.DataSources.InteractiveBrokers/
COPY src/QDMS.DataSources/QDMS.Server.DataSources.Tiingo/QDMS.Server.DataSources.Tiingo.csproj src/QDMS.DataSources/QDMS.Server.DataSources.Tiingo/
RUN dotnet restore src/QDMS.Host/QDMS.Host.csproj

COPY src/ src/
COPY --from=web /src/src/QDMS.Host/wwwroot/ui src/QDMS.Host/wwwroot/ui
RUN dotnet publish src/QDMS.Host/QDMS.Host.csproj -c Release --no-restore -o /app

# The Ubuntu-based image ships ICU and tzdata, which QDMS needs: exchanges store Windows time zone IDs
# ("Eastern Standard Time") that .NET converts to IANA IDs via ICU. Don't switch to an invariant/chiseled image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Npgsql probes for GSSAPI on connect and logs an error if it's missing
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
# the base image defaults to port 8080; QDMS sets its own URL from Qdms:httpPort
ENV ASPNETCORE_HTTP_PORTS=
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/logs && chown $APP_UID /app/logs
USER $APP_UID

# HTTP API, historical data (ZMQ), real-time pub/req (ZMQ)
EXPOSE 5559 5555 5556 5557
ENTRYPOINT ["dotnet", "QDMS.Host.dll"]
