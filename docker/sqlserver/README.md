# SQL Server

Arquivos de apoio para rodar o banco do FinTrack com SQL Server.

## Script de migrations

`fintrack-migrations.sql` é um script idempotente gerado pelo EF Core. Ele pode ser executado mais de uma vez sem recriar migrations já aplicadas.

Gerar novamente:

```powershell
.\scripts\database\generate-migration-script.ps1
```

## Docker

Subir SQL Server:

```powershell
docker compose --env-file .env.example up -d
```

Aplicar migrations:

```powershell
dotnet ef database update --project src\FinTrack.Infrastructure --startup-project src\FinTrack.Api --no-build
```
