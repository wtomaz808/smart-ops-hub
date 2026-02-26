FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY global.json ./
COPY src/SmartOpsHub.Core/SmartOpsHub.Core.csproj src/SmartOpsHub.Core/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.GitHub/SmartOpsHub.Mcp.GitHub.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.GitHub/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Azure/SmartOpsHub.Mcp.Azure.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Azure/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Ado/SmartOpsHub.Mcp.Ado.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Ado/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.DotNet/SmartOpsHub.Mcp.DotNet.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.DotNet/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.AI/SmartOpsHub.Mcp.AI.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.AI/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.DevOps/SmartOpsHub.Mcp.DevOps.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.DevOps/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Personal/SmartOpsHub.Mcp.Personal.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Personal/
COPY src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Gateway/SmartOpsHub.Mcp.Gateway.csproj src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Gateway/
RUN dotnet restore src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Gateway/SmartOpsHub.Mcp.Gateway.csproj

COPY src/SmartOpsHub.Core/ src/SmartOpsHub.Core/
COPY src/SmartOpsHub.Mcp/ src/SmartOpsHub.Mcp/
RUN dotnet build src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Gateway/SmartOpsHub.Mcp.Gateway.csproj -c Release --no-restore
RUN dotnet publish src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Gateway/SmartOpsHub.Mcp.Gateway.csproj -c Release --no-build -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["dotnet", "SmartOpsHub.Mcp.Gateway.dll", "--urls", "http://localhost:8080/health"]

ENTRYPOINT ["dotnet", "SmartOpsHub.Mcp.Gateway.dll"]
