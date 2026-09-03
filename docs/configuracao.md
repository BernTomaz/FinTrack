# Configuração Local

Instruções para executar o FinTrack localmente.

## Pré-requisitos

- .NET 10 SDK
- Node.js 20.19
- Angular CLI
- Docker Desktop, se for usar banco em container
- SQL Server local ou LocalDB, se não for usar Docker

## Backend

Solução: `FinTrack.slnx`.

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

O layout é responsivo, com prioridade para boa experiência mobile.

Comandos:

```powershell
cd src\FinTrack.Web
npm install
npm start
```

### Frontend via Docker

```powershell
docker compose up web
```

Endereço:

- Web: `http://localhost:4200`

## Aplicação via Docker

Para subir frontend, API e SQL Server:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Na primeira execução, a API aplica as migrations automaticamente quando o SQL Server ficar disponível.

Endereços:

- Web: `http://localhost:4200`
- API: `http://localhost:5080`
- Swagger UI: `http://localhost:5080/swagger`

## Banco

O banco principal será SQL Server. O projeto pode usar SQL Server no Docker, SQL Server local ou LocalDB.

### Docker

```powershell
Copy-Item .env.example .env
docker compose up -d sqlserver
```

Se a API também estiver rodando pelo Docker, ela aplica as migrations automaticamente.

Connection string padrão:

```text
Server=localhost,1433;Database=FinTrackDb;User Id=sa;Password=Your_strong_password123;Encrypt=False;TrustServerCertificate=True
```

### SQL Server local

```powershell
dotnet ef database update --project src\FinTrack.Infrastructure --startup-project src\FinTrack.Api --no-build --connection "Server=localhost;Database=FinTrackDb;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"
```

Se o servidor local tiver outro nome, troque `localhost` pelo nome exibido no SSMS.

### LocalDB

```powershell
dotnet ef database update --project src\FinTrack.Infrastructure --startup-project src\FinTrack.Api --no-build --connection "Server=(localdb)\MSSQLLocalDB;Database=FinTrackDb;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"
```

Para rodar a API com outro banco sem editar `appsettings.json`, defina `ConnectionStrings__DefaultConnection` como variável de ambiente ou use user-secrets.

## Segredos Locais

A chave JWT e senhas locais não devem ser versionadas. Usar user-secrets no backend e `.env` local para Docker.

## CI

O repositório possui GitHub Actions em `.github/workflows/ci.yml`.

O workflow executa:

- Restore, build e testes do backend.
- Instalação e build de produção do frontend.
