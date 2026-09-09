# Endpoints

## Auth

```text
POST /auth/register
POST /auth/login
GET /auth/me
PUT /auth/me
PUT /auth/password
```

Observações:

- `PUT /auth/me` atualiza o nome do usuário autenticado e retorna um novo JWT com os dados atualizados.
- `PUT /auth/password` valida a senha atual antes de salvar o novo hash de senha.

## Accounts

```text
GET /accounts
GET /accounts/{id}
POST /accounts
PUT /accounts/{id}
DELETE /accounts/{id}
```

Observações:

- `POST /accounts` e `PUT /accounts/{id}` aceitam `openingDate`. Quando não informado, a data atual é usada.
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
- A data do lançamento precisa ser igual ou posterior à data de início da conta.
- Após excluir lançamentos vinculados a uma conta ou categoria, a conta ou categoria pode ser removida.

Filtros:

```text
GET /transactions?year=2026&month=8&type=Expense&categoryId=1&accountId=2
```

## Dashboard

```text
GET /dashboard/monthly?year=2026&month=8
```

Observação:

- O saldo atual considera, até o fim do mês selecionado, os saldos iniciais das contas já iniciadas mais receitas menos despesas.

## Exports

```text
GET /exports/transactions.csv?year=2026&month=8
```

