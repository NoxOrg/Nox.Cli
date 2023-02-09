FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8000

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Nox.Cli.Server/Nox.Cli.Server.csproj", "Nox.Cli.Server/"]
COPY ["Nox.Cli.Plugins/Nox.Cli.Plugin.Network/Nox.Cli.Plugin.Network.csproj", "Nox.Cli.Plugins/Nox.Cli.Plugin.Network/"]
COPY ["Nox.Cli.Abstractions/Nox.Cli.Abstractions.csproj", "Nox.Cli.Abstractions/"]
COPY ["Nox.Cli.Shared.DTO/Nox.Cli.Shared.DTO.csproj", "Nox.Cli.Shared.DTO/"]
COPY ["Nox.Cli.Server.Cache/Nox.Cli.Server.Cache.csproj", "Nox.Cli.Server.Cache/"]
COPY ["Nox.Cli.Plugins/Nox.Cli.Plugin.AzDevOps/Nox.Cli.Plugin.AzDevOps.csproj", "Nox.Cli.Plugins/Nox.Cli.Plugin.AzDevOps/"]
COPY ["Nox.Cli.Plugins/Nox.Cli.Plugin.AzureAd/Nox.Cli.Plugin.AzureAd.csproj", "Nox.Cli.Plugins/Nox.Cli.Plugin.AzureAd/"]
COPY ["Nox.Cli.Plugin.Core/Nox.Cli.Plugin.Core.csproj", "Nox.Cli.Plugin.Core/"]
COPY ["Nox.Cli.Plugins/Nox.Cli.Plugin.File/Nox.Cli.Plugin.File.csproj", "Nox.Cli.Plugins/Nox.Cli.Plugin.File/"]
COPY ["Nox.Cli.Plugins/Nox.Cli.Plugin.Postgres/Nox.Cli.Plugin.Postgres.csproj", "Nox.Cli.Plugins/Nox.Cli.Plugin.Postgres/"]
COPY ["Nox.Cli.Plugins/Nox.Cli.Plugin.Powershell/Nox.Cli.Plugin.Powershell.csproj", "Nox.Cli.Plugins/Nox.Cli.Plugin.Powershell/"]
COPY ["Nox.Cli.Plugins/Nox.Cli.Plugin.Console/Nox.Cli.Plugin.Console.csproj", "Nox.Cli.Plugins/Nox.Cli.Plugin.Console/"]
COPY ["Nox.Cli.Helpers/Nox.Cli.Helpers.csproj", "Nox.Cli.Helpers/"]
RUN dotnet restore "Nox.Cli.Server/Nox.Cli.Server.csproj"
COPY . .
WORKDIR "/src/Nox.Cli.Server"
RUN dotnet build "Nox.Cli.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nox.Cli.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nox.Cli.Server.dll"]
