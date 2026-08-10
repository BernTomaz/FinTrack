# Configuração Local

Este arquivo será atualizado quando o código for criado.

## Pré-requisitos

- .NET 10 SDK
- Node.js 20.19
- Angular CLI
- Docker Desktop
- SQL Server via Docker ou instalacao local

## Backend

Solution: `FinTrack.slnx`.

Comandos:

```powershell
dotnet restore FinTrack.slnx -m:1
dotnet build FinTrack.slnx --no-restore -m:1
dotnet run --project src\FinTrack.Api
```

Endereços locais:

- API: `http://localhost:5080`
- Health check: `http://localhost:5080/health`
- OpenAPI: `http://localhost:5080/openapi/v1.json`
- Swagger UI: `http://localhost:5080/swagger`

## Frontend

O layout deve ser responsivo desde o inicio, com prioridade para boa experiência mobile.

Comandos:

```powershell
cd src\FinTrack.Web
npm install
npm start
```

## Banco

O banco principal será SQL Server.

Uso com Docker Compose:

```powershell
Copy-Item .env.example .env
docker compose up -d
```

## Segredos Locais

A chave JWT e senhas locais não devem ser versionadas. Usar user-secrets no backend e `.env` local para Docker.
