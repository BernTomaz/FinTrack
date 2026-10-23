# Endpoints

## Validações financeiras

- Criação e edição de contas, categorias e lançamentos rejeitam tipos inexistentes com `400 Bad Request`.
- Saldos iniciais e valores de lançamentos aceitam até duas casas decimais, sem arredondamento silencioso. Valores fora da precisão `decimal(18,2)` retornam `400 Bad Request` antes da gravação.
- O saldo inicial pode ser negativo. O valor de um lançamento precisa ser maior que zero.

## Auth

```text
POST /auth/register
POST /auth/login
GET /auth/me
PUT /auth/me
PUT /auth/password
GET /auth/admin/locked-users
POST /auth/admin/users/{id}/reset-password
```

Observações:

- `PUT /auth/me` atualiza o nome do usuário autenticado e retorna um novo JWT com os dados atualizados.
- `PUT /auth/password` valida a senha atual antes de salvar o novo hash de senha.
- `POST /auth/login` retorna `423 Locked` após excesso de tentativas inválidas.
- O primeiro usuário cadastrado é administrador.
- `GET /auth/admin/locked-users` e `POST /auth/admin/users/{id}/reset-password` exigem administrador.

## Accounts

```text
GET /accounts
GET /accounts/{id}
POST /accounts
PUT /accounts/{id}
DELETE /accounts/{id}
```

Observações:

- `POST /accounts` e `PUT /accounts/{id}` aceitam `openingDate`. Quando não informado, o cadastro usa a data atual e a edição mantém a data existente.
- `PUT /accounts/{id}` retorna `409 Conflict` se a nova data de início for posterior a algum lançamento da conta.
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

- `PUT /categories/{id}` retorna `409 Conflict` se o novo tipo for incompatível com lançamentos vinculados. Renomear a categoria continua permitido.
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

A listagem retorna `{ items, totalCount, page, pageSize }`. O padrão é `page=1` e `pageSize=50`, com limite de 100 itens por página. Páginas além do fim retornam `items: []`.

Use `search` (até 160 caracteres) para descrição, nome de conta/categoria ou data exata no formato `yyyy-MM-dd`; `sort=date|description|category|type|amount` e `direction=asc|desc` para ordenar antes da paginação. O padrão é data decrescente, com desempate por criação e identificador. Filtros e contagem respeitam o usuário autenticado.

Desde 20/10/2026, clientes que esperavam um array devem ler `items` e percorrer as páginas. A exportação CSV continua retornando todos os lançamentos dos filtros, sem paginação.


## Dashboard

```text
GET /dashboard/monthly?year=2026&month=8
```

Observação:

- O saldo atual considera, até o fim do mês selecionado, os saldos iniciais das contas já iniciadas mais receitas menos despesas.

## Exports

```text
GET /exports/transactions.csv?year=2026&month=8
GET /exports/transactions.csv?startDate=2026-08-01&endDate=2026-08-31
```

Observações:

- Sem `startDate` ou `endDate`, a exportação usa `year` e `month`.
- Com `startDate` ou `endDate`, a exportação usa o intervalo informado.

