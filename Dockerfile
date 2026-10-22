# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore primeiro, só com os .csproj: a camada fica em cache enquanto as dependências não mudam
COPY src/MeuApp.Shared/MeuApp.Shared.csproj src/MeuApp.Shared/
COPY src/MeuApp.Client/MeuApp.Client.csproj src/MeuApp.Client/
COPY src/MeuApp.Server/MeuApp.Server.csproj src/MeuApp.Server/
RUN dotnet restore src/MeuApp.Server/MeuApp.Server.csproj

COPY src/ src/
# Publicar o Server publica o Client (WASM) junto, em wwwroot
RUN dotnet publish src/MeuApp.Server/MeuApp.Server.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Fallback quando PORT não existe; no Render o Program.cs usa a PORT
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuário sem privilégios que já vem na imagem oficial
USER $APP_UID

ENTRYPOINT ["dotnet", "MeuApp.Server.dll"]
