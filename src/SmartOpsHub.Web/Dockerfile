FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

COPY Directory.Build.props global.json ./
COPY src/SmartOpsHub.Core/SmartOpsHub.Core.csproj src/SmartOpsHub.Core/
COPY src/SmartOpsHub.Infrastructure/SmartOpsHub.Infrastructure.csproj src/SmartOpsHub.Infrastructure/
COPY src/SmartOpsHub.Web/SmartOpsHub.Web.csproj src/SmartOpsHub.Web/
RUN dotnet restore src/SmartOpsHub.Web/SmartOpsHub.Web.csproj

COPY src/SmartOpsHub.Core/ src/SmartOpsHub.Core/
COPY src/SmartOpsHub.Infrastructure/ src/SmartOpsHub.Infrastructure/
COPY src/SmartOpsHub.Web/ src/SmartOpsHub.Web/
RUN dotnet publish src/SmartOpsHub.Web/SmartOpsHub.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartOpsHub.Web.dll"]
