# Banco de Dados

## Visão Geral

O FinTrack usa SQL Server com Entity Framework Core.

## Entidades Principais

- Users
- Accounts
- Categories
- Transactions

## Migrations

As migrations ficam no projeto `FinTrack.Infrastructure`.

Aplicar migrations manualmente:

```powershell
dotnet ef database update --project src\FinTrack.Infrastructure --startup-project src\FinTrack.Api --no-build
```

Gerar script SQL versionado:

```powershell
.\scripts\database\generate-migration-script.ps1
```

## Docker

O Docker Compose sobe SQL Server e a API aplica migrations automaticamente ao iniciar.

O script versionado fica em:

```text
docker/sqlserver/fintrack-migrations.sql
```

## Regras Persistidas

- Contas possuem saldo inicial e data de início.
- Lançamentos possuem descrição com limite alinhado entre API e banco.
- Contas e categorias com lançamentos vinculados não devem ser removidas diretamente.
