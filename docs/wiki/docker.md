# Docker

## Status

Implementado para desenvolvimento e demonstração local.

## Containers

- API ASP.NET Core
- Frontend Angular servido por Nginx
- SQL Server

## Objetivo

Permitir execução local padronizada com API, frontend e banco.

## Comandos

Crie um `.env` local a partir do `.env.example`:

```powershell
Copy-Item .env.example .env
```

Subir a aplicação:

```powershell
docker compose build
docker compose up -d
```

Ou reconstruir e iniciar em um único comando:

```powershell
docker compose up -d --build
```

Parar mantendo os dados:

```powershell
docker compose down
```

Parar e apagar o volume do banco:

```powershell
docker compose down -v
```

Use `-v` com cuidado, porque remove os dados locais do SQL Server.

## URLs locais

- Frontend: `http://localhost:4200`
- API: `http://localhost:5080`
- Health check: `http://localhost:5080/health`
- Swagger UI: `http://localhost:5080/swagger`

## Migrations

A API aplica migrations automaticamente ao iniciar pelo Docker Compose.
