FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

COPY ["./<repo-name>/<repo-name>.csproj", "<repo-name>/"]
RUN dotnet restore "./<repo-name>/<repo-name>.csproj"
COPY . .
WORKDIR "/<repo-name>"
RUN dotnet build "<repo-name>.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "<repo-name>.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "<repo-name>.dll"]