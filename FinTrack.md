# FinTrack

Sistema web para controle financeiro pessoal, com foco em registrar receitas, despesas, contas e acompanhar a situacao mensal do dinheiro.

## Objetivo

Permitir que o usuario organize suas movimentacoes financeiras, visualize resumo mensal e entenda seus gastos por categoria.

## Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core
- SQL Server
- xUnit
- FluentAssertions

### Frontend

- Angular
- TypeScript
- Reactive Forms
- HttpClient

### Infraestrutura

- Docker
- Docker Compose
- README com instrucoes de execucao

## MVP

O MVP deve conter apenas o necessario para o sistema funcionar bem.

### Funcionalidades

- Cadastro de usuario
- Login
- Cadastro de contas financeiras
- Cadastro de categorias
- Cadastro de lancamentos financeiros
- Listagem e filtros de lancamentos
- Dashboard mensal
- Exportacao CSV de lancamentos

## Fora do MVP

Nao implementar agora:

- Integracao bancaria
- Pagamento online
- IA
- OCR
- Upload de comprovantes
- Notificacoes por email
- App mobile
- Multi-moeda
- Compartilhamento de contas entre usuarios
- Assinatura paga

Esses recursos podem ser pensados depois que o MVP estiver pronto.

## Entidades

### User

Representa o usuario dono dos dados financeiros.

Campos:

- Id
- Name
- Email
- PasswordHash
- CreatedAt

Regras:

- Email deve ser unico
- Senha deve ser armazenada como hash
- Usuario so pode acessar os proprios dados

### Account

Representa uma conta financeira do usuario.

Exemplos:

- Carteira
- Conta corrente
- Conta poupanca
- Cartao de credito

Campos:

- Id
- UserId
- Name
- Type
- InitialBalance
- CreatedAt

Tipos sugeridos:

- Wallet
- Checking
- Savings
- CreditCard

Regras:

- Conta pertence a um usuario
- Nome da conta e obrigatorio
- Saldo inicial pode ser zero

### Category

Representa uma categoria de receita ou despesa.

Exemplos:

- Salario
- Mercado
- Aluguel
- Transporte
- Lazer
- Saude

Campos:

- Id
- UserId
- Name
- Type
- CreatedAt

Tipos:

- Income
- Expense

Regras:

- Categoria pertence a um usuario
- Categoria deve ser de receita ou despesa
- Nome e obrigatorio

### Transaction

Representa uma movimentacao financeira.

Campos:

- Id
- UserId
- AccountId
- CategoryId
- Type
- Amount
- Date
- Description
- CreatedAt

Tipos:

- Income
- Expense

Regras:

- Valor deve ser maior que zero
- Data e obrigatoria
- Lancamento pertence a uma conta
- Lancamento pertence a uma categoria
- Tipo do lancamento deve ser compativel com o tipo da categoria
- Usuario so pode usar contas e categorias dele

## Regras de Negocio

### Saldo

Saldo atual de uma conta:

```text
saldo inicial + receitas - despesas
```

### Dashboard mensal

O dashboard deve mostrar:

- Total de receitas do mes
- Total de despesas do mes
- Saldo do mes
- Saldo geral atual
- Gastos por categoria
- Ultimos lancamentos

### Filtros de lancamentos

Filtros necessarios:

- Mes
- Ano
- Tipo
- Categoria
- Conta

## Endpoints

### Auth

```text
POST /auth/register
POST /auth/login
```

### Accounts

```text
GET /accounts
GET /accounts/{id}
POST /accounts
PUT /accounts/{id}
DELETE /accounts/{id}
```

### Categories

```text
GET /categories
GET /categories/{id}
POST /categories
PUT /categories/{id}
DELETE /categories/{id}
```

### Transactions

```text
GET /transactions
GET /transactions/{id}
POST /transactions
PUT /transactions/{id}
DELETE /transactions/{id}
```

Query params sugeridos para listagem:

```text
GET /transactions?year=2026&month=8&type=Expense&categoryId=1&accountId=2
```

### Dashboard

```text
GET /dashboard/monthly?year=2026&month=8
```

### Exports

```text
GET /exports/transactions.csv?year=2026&month=8
```

## Estrutura Recomendada

```text
FinTrack/
  src/
    FinTrack.Api/
    FinTrack.Application/
    FinTrack.Domain/
    FinTrack.Infrastructure/
  tests/
    FinTrack.Tests/
  docker-compose.yml
  README.md
```

## Camadas

### FinTrack.Api

Responsavel por:

- Controllers
- Autenticacao
- Configuracao da API
- Swagger

Controllers devem ser finos.

### FinTrack.Application

Responsavel por:

- Services
- DTOs
- Validacoes de caso de uso
- Regras de aplicacao

### FinTrack.Domain

Responsavel por:

- Entidades
- Enums
- Regras centrais do dominio

### FinTrack.Infrastructure

Responsavel por:

- DbContext
- Migrations
- Repositorios, se forem necessarios
- Configuracao do Entity Framework

## DTOs Iniciais

### RegisterRequest

- Name
- Email
- Password

### LoginRequest

- Email
- Password

### AccountRequest

- Name
- Type
- InitialBalance

### CategoryRequest

- Name
- Type

### TransactionRequest

- AccountId
- CategoryId
- Type
- Amount
- Date
- Description

### MonthlyDashboardResponse

- IncomeTotal
- ExpenseTotal
- MonthBalance
- CurrentBalance
- ExpensesByCategory
- RecentTransactions

## Telas do Frontend

### Login

- Email
- Senha
- Link para cadastro

### Cadastro

- Nome
- Email
- Senha

### Dashboard

- Cards de resumo
- Grafico de gastos por categoria
- Lista dos ultimos lancamentos
- Filtro por mes e ano

### Contas

- Listagem de contas
- Criar conta
- Editar conta
- Excluir conta

### Categorias

- Listagem de categorias
- Criar categoria
- Editar categoria
- Excluir categoria

### Lancamentos

- Listagem de lancamentos
- Criar lancamento
- Editar lancamento
- Excluir lancamento
- Filtros

## Testes

Testes minimos:

- Criar conta com dados validos
- Impedir transacao com valor menor ou igual a zero
- Impedir usuario de usar conta de outro usuario
- Impedir usuario de usar categoria de outro usuario
- Calcular total de receitas do mes
- Calcular total de despesas do mes
- Calcular saldo mensal
- Exportar CSV com lancamentos filtrados

## Etapas de Implementacao

### 1. Estrutura

- Criar solution
- Criar projetos da API, Application, Domain, Infrastructure e Tests
- Referenciar projetos
- Configurar Swagger

### 2. Dominio

- Criar entidades
- Criar enums
- Criar regras basicas

### 3. Banco

- Configurar SQL Server
- Criar DbContext
- Criar migrations
- Criar docker-compose

### 4. Auth

- Cadastro
- Login
- JWT
- Protecao de endpoints

### 5. Contas

- CRUD de contas
- Regras de usuario dono

### 6. Categorias

- CRUD de categorias
- Regras de tipo

### 7. Lancamentos

- CRUD de lancamentos
- Filtros
- Validacoes

### 8. Dashboard

- Resumo mensal
- Gastos por categoria
- Ultimos lancamentos

### 9. Exportacao

- CSV de lancamentos filtrados

### 10. Frontend

- Login e cadastro
- Dashboard
- Contas
- Categorias
- Lancamentos

### 11. Testes

- Testes de regras de negocio
- Testes de services principais

### 12. Finalizacao

- README
- Prints
- Docker
- Deploy opcional

## README Deve Conter

- Nome do projeto
- Descricao curta
- Tecnologias usadas
- Funcionalidades do MVP
- Como rodar backend
- Como rodar frontend
- Como rodar banco com Docker
- Como rodar testes
- Prints ou GIFs
- Proximos passos

## Criterio de Pronto

O FinTrack MVP esta pronto quando:

- Usuario consegue cadastrar e logar
- Usuario consegue criar contas
- Usuario consegue criar categorias
- Usuario consegue criar receitas e despesas
- Dashboard mensal mostra os valores corretos
- Lancamentos podem ser filtrados
- CSV pode ser exportado
- Testes principais passam
- README explica como rodar o projeto

