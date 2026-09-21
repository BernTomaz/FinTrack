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

- Angular 20
- TypeScript
- Reactive Forms
- HttpClient
- Layout responsivo mobile-first

### Infraestrutura

- Docker
- Docker Compose
- GitHub Actions
- README com instruções de execução

## MVP

O MVP contém apenas o necessário para o sistema funcionar bem.

### Funcionalidades

- Cadastro de usuário
- Login
- Cadastro de contas financeiras
- Cadastro de categorias
- Cadastro de lançamentos financeiros
- Listagem e filtros de lançamentos
- Dashboard mensal
- Gráfico de fluxo de caixa com dados reais
- Exportação CSV de lançamentos
- Exclusão de lançamentos
- Bloqueio de exclusão de contas e categorias com lançamentos vinculados
- Perfil editável
- Alteração de senha

## Fora do MVP

Não implementar agora:

- Integração bancária
- Pagamento online
- IA
- OCR
- Upload de comprovantes
- Notificações por e-mail
- App mobile
- Multi-moeda
- Compartilhamento de contas entre usuários
- Assinatura paga

Esses recursos podem ser pensados depois do MVP.

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

- E-mail deve ser único
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
- OpeningDate
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
- Data de início define quando a conta entra no cálculo de saldo atual
- Conta com lançamentos vinculados não pode ser excluída diretamente

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
- Categoria com lançamentos vinculados não pode ser excluída diretamente

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
- Data do lançamento não pode ser anterior à data de início da conta

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
- Saldo atual
- Gastos por categoria
- Últimos lançamentos
- Fluxo de caixa com dados reais

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
GET /auth/me
PUT /auth/me
PUT /auth/password
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

Query params da listagem:

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

## Estrutura do projeto

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
  docker/
  scripts/
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
- Health check

Endpoints devem manter a lógica de negócio nos services.

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

## DTOs principais

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
- OpeningDate

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

### Perfil

- Nome
- E-mail somente leitura
- Alteração de senha

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
- Impedir lançamento antes da data de início da conta
- Impedir exclusão de conta ou categoria com lançamentos vinculados

## Etapas de implementação

### 1. Estrutura

- Criar solução `FinTrack.slnx`
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

## README contém

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

## Critério de pronto

O FinTrack MVP está pronto quando:

- Usuário consegue se cadastrar e fazer login.
- Usuário consegue criar contas.
- Usuário consegue criar categorias.
- Usuário consegue criar receitas e despesas.
- Dashboard mensal mostra os valores corretos.
- Lançamentos podem ser filtrados.
- CSV pode ser exportado.
- Testes principais passam.
- README explica como rodar o projeto.

