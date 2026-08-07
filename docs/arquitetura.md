# Arquitetura

O FinTrack usa uma divisão simples em camadas, seguindo a base do GradeFlow.

## Camadas

### FinTrack.Api

Entrada HTTP da aplicação.

Responsabilidades:

- Controllers
- Autenticação JWT
- Swagger
- Configuração da API
- Health check

### FinTrack.Application

Casos de uso da aplicação.

Responsabilidades:

- Services
- DTOs
- Validações de fluxo
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

## Regra de Dependência

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
Api -> Infrastructure
Web -> Api
```

## Decisão Inicial

Começar com services diretos e DTOs simples. Repositórios só entram se o acesso a dados começar a repetir lógica real.

A solution .NET será criada como `FinTrack.slnx`, não `.sln`.
