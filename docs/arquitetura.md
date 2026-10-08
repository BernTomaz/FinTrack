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
