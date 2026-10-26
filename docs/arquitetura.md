# Arquitetura

O FinTrack usa uma divisão simples em camadas, focada em manter o MVP legível e fácil de evoluir.

## Camadas

### FinTrack.Api

Entrada HTTP da aplicação.

Responsabilidades:

- Endpoints HTTP minimalistas
- Autenticação JWT
- Swagger
- Configuração da API
- Health check

### FinTrack.Application

Contratos simples compartilhados pela API.

Responsabilidades:

- DTOs
- Contratos usados pela API

### FinTrack.Domain

Regras centrais do negócio.

Responsabilidades:

- Entidades
- Enums
- Regras que não dependem de banco ou HTTP

### FinTrack.Infrastructure

Acesso a dados e integrações locais.

Responsabilidades:

- DbContext
- Entity Framework Core
- Migrations
- Repositórios, se forem necessários

### FinTrack.Web

Frontend Angular.

Responsabilidades:

- Telas
- Rotas
- Formulários
- Consumo da API
- Estado local simples
- Layout responsivo mobile-first

## Regra de dependência

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
Api -> Infrastructure
Web -> Api
```

## Decisão arquitetural

Começar com endpoints diretos e DTOs simples. Services e repositórios só entram se reduzirem repetição real.

A solução .NET usa `FinTrack.slnx`, não `.sln`.

## Organização interna

O frontend agrupa os componentes por funcionalidade em `auth/`, `dashboard/`, `accounts/`, `categories/`, `transactions/`, `reports/` e `settings/`. Funções pequenas usadas por mais de uma tela ficam em `shared/`, com seus testes ao lado.

`AuthComponent` cuida dos formulários, validações, mensagens de erro e requisições de login e cadastro. Ele comunica a autenticação concluída ao `App`, que mantém a sessão, a navegação e o carregamento financeiro. O dashboard tem seu HTML separado do TypeScript; componentes pequenos mantêm templates inline.

Os testes de fluxo da API estão separados por assunto em `tests/FinTrack.Tests/`. A fábrica da API, os dados e verificações compartilhados e o atributo de teste SQL Server ficam em `Fixtures/`, sem criar uma hierarquia de classes de teste.

Os documentos de arquitetura, testes e roadmap em `docs/` são as fontes principais. As páginas correspondentes em `docs/wiki/` apontam para eles para evitar instruções divergentes.
