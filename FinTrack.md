# FinTrack

Sistema web para controle financeiro pessoal, com foco em registrar receitas, despesas, contas e acompanhar a situação mensal do dinheiro.

## Objetivo

Permitir que o usuário organize suas movimentações financeiras, visualize resumo mensal e entenda seus gastos por categoria.

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
- README com instruções de execução

## MVP

O MVP deve conter apenas o necessário para o sistema funcionar bem.

### Funcionalidades

- Cadastro de usuário
- Login
- Cadastro de contas financeiras
- Cadastro de categorias
- Cadastro de lançamentos financeiros
- Listagem e filtros de lançamentos
- Dashboard mensal
- Exportação CSV de lançamentos

## Fora do MVP

Não implementar agora:

- Integração bancária
- Pagamento online
- IA
- OCR
- Upload de comprovantes
- Notificações por email
- App mobile
- Multi-moeda
- Compartilhamento de contas entre usuários
- Assinatura paga

Esses recursos podem ser pensados depois que o MVP estiver pronto.

## Entidades

### User

Representa o usuário dono dos dados financeiros.

Campos:

- Id
- Name
- Email
- PasswordHash
- CreatedAt

Regras:

- Email deve ser único
- Senha deve ser armazenada como hash
- Usuário só pode acessar os próprios dados

### Account

Representa uma conta financeira do usuário.

Exemplos:

- Carteira
- Conta corrente
- Conta poupança
- Cartão de crédito

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

- Conta pertence a um usuário
- Nome da conta é obrigatório
- Saldo inicial pode ser zero

### Category

Representa uma categoria de receita ou despesa.

Exemplos:

- Salário
- Mercado
- Aluguel
- Transporte
- Lazer
- Saúde

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

- Categoria pertence a um usuário
- Categoria deve ser de receita ou despesa
- Nome é obrigatório

### Transaction

Representa uma movimentação financeira.

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
- Data é obrigatória
- Lançamento pertence a uma conta
- Lançamento pertence a uma categoria
- Tipo do lançamento deve ser compatível com o tipo da categoria
- Usuário só pode usar contas e categorias dele

## Regras de Negócio

### Saldo

Saldo atual de uma conta:

```text
saldo inicial + receitas - despesas
```

### Dashboard mensal

O dashboard deve mostrar:

- Total de receitas do mês
- Total de despesas do mês
- Saldo do mês
- Saldo geral atual
- Gastos por categoria
- Últimos lançamentos

### Filtros de lançamentos

Filtros necessários:

- Mês
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

Responsável por:

- Controllers
- Autenticação
- Configuração da API
- Swagger

Controllers devem ser finos.

### FinTrack.Application

Responsável por:

- Services
- DTOs
- Validações de caso de uso
- Regras de aplicação

### FinTrack.Domain

Responsável por:

- Entidades
- Enums
- Regras centrais do domínio

### FinTrack.Infrastructure

Responsável por:

- DbContext
- Migrations
- Repositórios, se forem necessários
- Configuração do Entity Framework

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
- Gráfico de gastos por categoria
- Lista dos últimos lançamentos
- Filtro por mês e ano

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

### Lançamentos

- Listagem de lançamentos
- Criar lançamento
- Editar lançamento
- Excluir lançamento
- Filtros

## Testes

Testes mínimos:

- Criar conta com dados válidos
- Impedir transação com valor menor ou igual a zero
- Impedir usuário de usar conta de outro usuário
- Impedir usuário de usar categoria de outro usuário
- Calcular total de receitas do mês
- Calcular total de despesas do mês
- Calcular saldo mensal
- Exportar CSV com lançamentos filtrados

## Etapas de Implementação

### 1. Estrutura

- Criar solution
- Criar projetos da API, Application, Domain, Infrastructure e Tests
- Referenciar projetos
- Configurar Swagger

### 2. Domínio

- Criar entidades
- Criar enums
- Criar regras básicas

### 3. Banco

- Configurar SQL Server
- Criar DbContext
- Criar migrations
- Criar docker-compose

### 4. Auth

- Cadastro
- Login
- JWT
- Proteção de endpoints

### 5. Contas

- CRUD de contas
- Regras de usuário dono

### 6. Categorias

- CRUD de categorias
- Regras de tipo

### 7. Lançamentos

- CRUD de lançamentos
- Filtros
- Validações

### 8. Dashboard

- Resumo mensal
- Gastos por categoria
- Últimos lançamentos

### 9. Exportação

- CSV de lançamentos filtrados

### 10. Frontend

- Login e cadastro
- Dashboard
- Contas
- Categorias
- Lançamentos

### 11. Testes

- Testes de regras de negócio
- Testes de services principais

### 12. Finalização

- README
- Prints
- Docker
- Deploy opcional

## README Deve Conter

- Nome do projeto
- Descrição curta
- Tecnologias usadas
- Funcionalidades do MVP
- Como rodar backend
- Como rodar frontend
- Como rodar banco com Docker
- Como rodar testes
- Prints ou GIFs
- Próximos passos

## Critério de Pronto

O FinTrack MVP está pronto quando:

- Usuário consegue cadastrar e logar
- Usuário consegue criar contas
- Usuário consegue criar categorias
- Usuário consegue criar receitas e despesas
- Dashboard mensal mostra os valores corretos
- Lançamentos podem ser filtrados
- CSV pode ser exportado
- Testes principais passam
- README explica como rodar o projeto

