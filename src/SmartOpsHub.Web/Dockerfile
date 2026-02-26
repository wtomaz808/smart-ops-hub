FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/SmartOpsHub.Core/SmartOpsHub.Core.csproj", "SmartOpsHub.Core/"]
COPY ["src/SmartOpsHub.Infrastructure/SmartOpsHub.Infrastructure.csproj", "SmartOpsHub.Infrastructure/"]
COPY ["src/SmartOpsHub.Web/SmartOpsHub.Web.csproj", "SmartOpsHub.Web/"]
RUN dotnet restore "SmartOpsHub.Web/SmartOpsHub.Web.csproj"

COPY src/SmartOpsHub.Core/ SmartOpsHub.Core/
COPY src/SmartOpsHub.Infrastructure/ SmartOpsHub.Infrastructure/
COPY src/SmartOpsHub.Web/ SmartOpsHub.Web/

WORKDIR /src/SmartOpsHub.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartOpsHub.Web.dll"]
