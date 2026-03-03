FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

COPY Directory.Build.props global.json ./
COPY src/SmartOpsHub.Core/SmartOpsHub.Core.csproj src/SmartOpsHub.Core/
COPY src/SmartOpsHub.Infrastructure/SmartOpsHub.Infrastructure.csproj src/SmartOpsHub.Infrastructure/
COPY src/SmartOpsHub.Api/SmartOpsHub.Api.csproj src/SmartOpsHub.Api/
RUN dotnet restore src/SmartOpsHub.Api/SmartOpsHub.Api.csproj

COPY src/SmartOpsHub.Core/ src/SmartOpsHub.Core/
COPY src/SmartOpsHub.Infrastructure/ src/SmartOpsHub.Infrastructure/
COPY src/SmartOpsHub.Api/ src/SmartOpsHub.Api/
RUN dotnet publish src/SmartOpsHub.Api/SmartOpsHub.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["dotnet", "SmartOpsHub.Api.dll", "--urls", "http://localhost:8080/health"]

ENTRYPOINT ["dotnet", "SmartOpsHub.Api.dll"]
