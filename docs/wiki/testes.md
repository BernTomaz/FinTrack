# Testes

## Visão geral

O FinTrack possui testes automatizados para regras de domínio, autenticação, infraestrutura e fluxo principal da API.

## Stack

- xUnit
- FluentAssertions
- ASP.NET Core TestHost

## Escopos

- Regras do domínio.
- Cadastro e login.
- Endpoints protegidos.
- Fluxo de contas, categorias, lançamentos, dashboard e CSV.
- Regras de exclusão de contas e categorias com vínculos.
- Bloqueio de lançamento antes da data de início da conta.

## Comando

```powershell
dotnet test tests\FinTrack.Tests\FinTrack.Tests.csproj --no-restore -m:1
```

## Validação de frontend

Build de produção:

```powershell
cd src\FinTrack.Web
npm run build
```
