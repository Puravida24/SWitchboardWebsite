FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/TheSwitchboard.Web/TheSwitchboard.Web.csproj src/TheSwitchboard.Web/
RUN dotnet restore src/TheSwitchboard.Web/TheSwitchboard.Web.csproj

COPY src/TheSwitchboard.Web/ src/TheSwitchboard.Web/
RUN dotnet publish src/TheSwitchboard.Web/TheSwitchboard.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "dotnet TheSwitchboard.Web.dll --urls http://+:${PORT:-8080}"]
