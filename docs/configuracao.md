# Configuração Local

Este arquivo será atualizado quando o código for criado.

## Pré-requisitos

- .NET 10 SDK
- Node.js LTS
- Angular CLI
- Docker Desktop
- SQL Server via Docker ou instalacao local

## Backend

Solution planejada: `FinTrack.slnx`.

Comandos planejados:

```powershell
dotnet restore
dotnet build
dotnet run --project src\FinTrack.Api
```

## Frontend

O layout deve ser responsivo desde o inicio, com prioridade para boa experiência mobile.

Comandos planejados:

```powershell
cd src\FinTrack.Web
npm install
npm start
```

## Banco

O banco principal será SQL Server.

Uso planejado com Docker Compose:

```powershell
docker compose up -d
```

## Segredos Locais

A chave JWT e senhas locais não devem ser versionadas. Usar user-secrets no backend e `.env` local para Docker.
