# FinTrack

Sistema web para controle financeiro pessoal, com cadastro de receitas, despesas, contas, categorias, dashboard mensal e exportação CSV.

## Documentação

- [Visão geral](docs/etapas/00-etapa-visao-geral.md)
- [Arquitetura](docs/arquitetura.md)
- [Configuração local](docs/configuracao.md)
- [Endpoints planejados](docs/endpoints.md)
- [Testes](docs/testes.md)
- [Roadmap](docs/roadmap.md)
- [Status do projeto](docs/status-projeto.md)
- [Fluxo geral](docs/fluxos/00-fluxo-geral.md)
- [Roadmap de implementação](docs/fluxos/10-roadmap-implementacao.md)

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

- Cadastro de usuário
- Login
- Cadastro de contas financeiras
- Cadastro de categorias
- Cadastro de lançamentos financeiros
- Listagem e filtros de lançamentos
- Dashboard mensal
- Exportação CSV de lançamentos

## Modo de Desenvolvimento

O projeto deve seguir uma abordagem simples: menos abstração, menos dependência e mais fluxo direto. Recursos fora do MVP ficam documentados para depois.

A solution .NET deve usar o formato `.slnx`.

## Status

Projeto em estruturação inicial. Ainda não há código de aplicação.
