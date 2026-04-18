FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/TheSwitchboard.Web/TheSwitchboard.Web.csproj src/TheSwitchboard.Web/
RUN dotnet restore src/TheSwitchboard.Web/TheSwitchboard.Web.csproj

COPY src/TheSwitchboard.Web/ src/TheSwitchboard.Web/
RUN dotnet publish src/TheSwitchboard.Web/TheSwitchboard.Web.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

# Install curl for the HEALTHCHECK probe. The aspnet:9.0 base image already
# ships with a non-root 'app' user (uid 1654) — we just switch to it below.
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

COPY --chown=app:app --from=build /app/publish .

USER app

EXPOSE 8080

# Matches railway.toml healthcheckPath=/health. Hits 127.0.0.1 inside the container.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://127.0.0.1:8080/health || exit 1

# Exec form, no shell. Program.cs reads PORT env var at startup and re-binds if set.
ENTRYPOINT ["dotnet", "TheSwitchboard.Web.dll"]
