# Endpoints Planejados

## Auth

```text
POST /auth/register
POST /auth/login
GET /auth/me
```

## Accounts

```text
GET /accounts
GET /accounts/{id}
POST /accounts
PUT /accounts/{id}
DELETE /accounts/{id}
```

## Categories

```text
GET /categories
GET /categories/{id}
POST /categories
PUT /categories/{id}
DELETE /categories/{id}
```

## Transactions

```text
GET /transactions
GET /transactions/{id}
POST /transactions
PUT /transactions/{id}
DELETE /transactions/{id}
```

Filtros planejados:

```text
GET /transactions?year=2026&month=8&type=Expense&categoryId=1&accountId=2
```

## Dashboard

```text
GET /dashboard/monthly?year=2026&month=8
```

## Exports

```text
GET /exports/transactions.csv?year=2026&month=8
```

