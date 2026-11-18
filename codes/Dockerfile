# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS source
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/ ./src/

FROM source AS publish-api
RUN dotnet restore src/Hospital.Api/Hospital.Api.csproj --locked-mode
RUN dotnet publish src/Hospital.Api/Hospital.Api.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM source AS publish-web
RUN dotnet restore src/Hospital.Web/Hospital.Web.csproj --locked-mode
RUN dotnet publish src/Hospital.Web/Hospital.Web.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

FROM runtime AS api
COPY --from=publish-api /out/ ./
ENTRYPOINT ["dotnet", "Hospital.Api.dll"]

FROM runtime AS web
COPY --from=publish-web /out/ ./
ENTRYPOINT ["dotnet", "Hospital.Web.dll"]
