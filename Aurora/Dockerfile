# =============================================================================
# Aurora Pet - imagem da API
# Multi-stage: o SDK compila e publica, a imagem final carrega apenas o runtime.
# O container roda como usuario sem privilegios (exigencia da entrega de DevOps).
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore em camada separada: so refaz o download quando o csproj muda.
COPY ["Aurora/Aurora.csproj", "Aurora/"]
RUN dotnet restore "Aurora/Aurora.csproj"

COPY . .
RUN dotnet publish "Aurora/Aurora.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Usuario sem privilegios administrativos.
RUN useradd --uid 10001 --create-home --shell /usr/sbin/nologin aurora

COPY --from=build /app/publish .

# Diretorio dos logs do Serilog precisa pertencer ao usuario da aplicacao.
RUN mkdir -p /app/logs && chown -R aurora:aurora /app

USER aurora

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# Segredos NAO vao na imagem: passe -e MongoDb__ConnectionString=... -e Jwt__Key=...
# (e, se usar Oracle, -e Database__Provider=Oracle -e ConnectionStrings__OracleConnection=...)
ENTRYPOINT ["dotnet", "Aurora.dll"]
