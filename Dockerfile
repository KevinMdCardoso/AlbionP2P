# ── Stage 1: Build & Publish ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj primeiro para aproveitar cache de camadas Docker
COPY AlbionP2P.sln .
COPY src/AlbionP2P.Domain/AlbionP2P.Domain.csproj                                         src/AlbionP2P.Domain/
COPY src/AlbionP2P.Application/AlbionP2P.Application.csproj                               src/AlbionP2P.Application/
COPY src/AlbionP2P.Infrastructure/AlbionP2P.Infrastructure.csproj                         src/AlbionP2P.Infrastructure/
COPY src/AlbionP2P.API/AlbionP2P.API.csproj                                               src/AlbionP2P.API/
COPY src/AlbionP2P.Web/AlbionP2P.Web.csproj                                               src/AlbionP2P.Web/
COPY tests/AlbionP2P.Application.Tests/AlbionP2P.Application.Tests.csproj                 tests/AlbionP2P.Application.Tests/
COPY tests/AlbionP2P.Domain.Tests/AlbionP2P.Domain.Tests.csproj                           tests/AlbionP2P.Domain.Tests/

RUN dotnet restore

# Copiar todo o código-fonte
COPY . .

# Publicar Blazor WASM — os arquivos estáticos ficam em /blazor-out/wwwroot
RUN dotnet publish src/AlbionP2P.Web/AlbionP2P.Web.csproj \
    -c Release -o /blazor-out --no-restore --nologo

# Publicar a API
RUN dotnet publish src/AlbionP2P.API/AlbionP2P.API.csproj \
    -c Release -o /api-out --no-restore --nologo

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar API publicada
COPY --from=build /api-out .

# Copiar arquivos estáticos do Blazor WASM para dentro do wwwroot da API
COPY --from=build /blazor-out/wwwroot ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "AlbionP2P.API.dll"]
