# Configuracao Local

Este arquivo sera atualizado quando o codigo for criado.

## Pre-requisitos

- .NET 10 SDK
- Node.js LTS
- Angular CLI
- Docker Desktop
- SQL Server via Docker ou instalacao local

## Backend

Comandos planejados:

```powershell
dotnet restore
dotnet build
dotnet run --project src\FinTrack.Api
```

## Frontend

Comandos planejados:

```powershell
cd src\FinTrack.Web
npm install
npm start
```

## Banco

O banco principal sera SQL Server.

Uso planejado com Docker Compose:

```powershell
docker compose up -d
```

## Segredos Locais

A chave JWT e senhas locais nao devem ser versionadas. Usar user-secrets no backend e `.env` local para Docker.

