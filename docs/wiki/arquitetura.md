# Arquitetura

## Visão geral

O FinTrack usa arquitetura em camadas para separar regras de negócio, casos de uso, persistência, exposição HTTP e interface web.

## Camadas

### Domain

Responsável pelas regras centrais do negócio:

- User
- Account
- Category
- Transaction
- AccountType
- CategoryType
- TransactionType

### Application

Responsável pelos casos de uso da aplicação:

- Services
- DTOs
- Validações de fluxo
- Contratos usados pela API

### Infrastructure

Responsável pelo acesso a dados:

- Entity Framework Core
- DbContext
- Migrations
- Configurações do banco

### API

Responsável pela exposição dos endpoints REST:

- Minimal APIs
- Autenticação JWT
- Swagger/OpenAPI
- Health check

### Web

Frontend Angular responsável pela interface do usuário:

- Telas e componentes
- Formulários reativos
- Consumo da API
- Sessão local simples
- Layout responsivo mobile-first

## Dependências

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
Api -> Infrastructure
Web -> Api
```

## Decisão arquitetural

O projeto começa com services diretos e DTOs simples. Repositórios e abstrações novas só entram se reduzirem repetição real.

A solução .NET usa `FinTrack.slnx`.
