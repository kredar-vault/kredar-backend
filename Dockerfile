FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Kredar.API/Kredar.API.csproj ./
RUN dotnet restore
COPY src/Kredar.API/ ./
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Run as the image's built-in non-root user (hardening; satisfies Trivy DS-0002).
USER $APP_UID
ENTRYPOINT ["dotnet", "Kredar.API.dll"]
