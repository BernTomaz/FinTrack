# Migrations

## Objetivo

Manter a evolucao do banco rastreavel.

## Fluxo Planejado

1. Alterar entidades ou configuracoes do EF Core.
2. Criar migration.
3. Revisar migration.
4. Aplicar no banco local.
5. Atualizar scripts de Docker quando existirem.

## Comandos

```powershell
dotnet ef migrations add NomeDaMigration --project src\FinTrack.Infrastructure --startup-project src\FinTrack.Api --output-dir Migrations --no-build
dotnet ef database update --project src\FinTrack.Infrastructure --startup-project src\FinTrack.Api --no-build
.\scripts\database\generate-migration-script.ps1
```

