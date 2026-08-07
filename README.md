# FinTrack

Sistema web para controle financeiro pessoal, com cadastro de receitas, despesas, contas, categorias, dashboard mensal e exportacao CSV.

## Documentacao

- [Visao geral](docs/etapas/00-etapa-visao-geral.md)
- [Arquitetura](docs/arquitetura.md)
- [Configuracao local](docs/configuracao.md)
- [Endpoints planejados](docs/endpoints.md)
- [Testes](docs/testes.md)
- [Roadmap](docs/roadmap.md)
- [Status do projeto](docs/status-projeto.md)
- [Fluxo geral](docs/fluxos/00-fluxo-geral.md)
- [Roadmap de implementacao](docs/fluxos/10-roadmap-implementacao.md)

## Stack Prevista

Backend:

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication

Frontend:

- Angular
- TypeScript
- Reactive Forms
- HttpClient
- Layout responsivo mobile-first

Testes:

- xUnit
- FluentAssertions

Infra:

- Docker
- Docker Compose

## Estrutura Inicial

```text
FinTrack/
  src/
    FinTrack.Api/
    FinTrack.Application/
    FinTrack.Domain/
    FinTrack.Infrastructure/
    FinTrack.Web/
  tests/
    FinTrack.Tests/
  docs/
    etapas/
    fluxos/
    operacao/
  docker/
    sqlserver/
  scripts/
    database/
```

## MVP

- Cadastro de usuario
- Login
- Cadastro de contas financeiras
- Cadastro de categorias
- Cadastro de lancamentos financeiros
- Listagem e filtros de lancamentos
- Dashboard mensal
- Exportacao CSV de lancamentos

## Modo de Desenvolvimento

O projeto deve seguir uma abordagem simples: menos abstracao, menos dependencia e mais fluxo direto. Recursos fora do MVP ficam documentados para depois.

A solution .NET deve usar o formato `.slnx`.

## Status

Projeto em estruturacao inicial. Ainda nao ha codigo de aplicacao.
