# Arquitetura

O FinTrack usa uma divisao simples em camadas, seguindo a base do GradeFlow.

## Camadas

### FinTrack.Api

Entrada HTTP da aplicacao.

Responsabilidades:

- Controllers
- Autenticacao JWT
- Swagger
- Configuracao da API
- Health check

### FinTrack.Application

Casos de uso da aplicacao.

Responsabilidades:

- Services
- DTOs
- Validacoes de fluxo
- Contratos usados pela API

### FinTrack.Domain

Regras centrais do negocio.

Responsabilidades:

- Entidades
- Enums
- Regras que nao dependem de banco ou HTTP

### FinTrack.Infrastructure

Acesso a dados e integracoes locais.

Responsabilidades:

- DbContext
- Entity Framework Core
- Migrations
- Repositorios, se forem necessarios

### FinTrack.Web

Frontend Angular.

Responsabilidades:

- Telas
- Rotas
- Formularios
- Consumo da API
- Estado local simples

## Regra de Dependencia

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
Api -> Infrastructure
Web -> Api
```

## Decisao Inicial

Comecar com services diretos e DTOs simples. Repositorios so entram se o acesso a dados comecar a repetir logica real.

