# Endpoints

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

Observações:

- `DELETE /accounts/{id}` retorna `404 Not Found` quando a conta não existe ou não pertence ao usuário autenticado.
- `DELETE /accounts/{id}` retorna `409 Conflict` quando a conta possui lançamentos vinculados.
- Para excluir uma conta com lançamentos, exclua primeiro os lançamentos relacionados.

## Categories

```text
GET /categories
GET /categories/{id}
POST /categories
PUT /categories/{id}
DELETE /categories/{id}
```

Observações:

- `DELETE /categories/{id}` retorna `404 Not Found` quando a categoria não existe ou não pertence ao usuário autenticado.
- `DELETE /categories/{id}` retorna `409 Conflict` quando a categoria possui lançamentos vinculados.
- Para excluir uma categoria com lançamentos, exclua primeiro os lançamentos relacionados.

## Transactions

```text
GET /transactions
GET /transactions/{id}
POST /transactions
PUT /transactions/{id}
DELETE /transactions/{id}
```

Observações:

- `DELETE /transactions/{id}` remove um lançamento do usuário autenticado.
- Após excluir lançamentos vinculados a uma conta ou categoria, a conta ou categoria pode ser removida.

Filtros:

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

