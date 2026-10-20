# API

## Visão Geral

A API expõe endpoints REST para autenticação, perfil, administração básica, contas, categorias, lançamentos, dashboard, status e exportação CSV.

## Endpoints Principais

| Recurso | Endpoints |
| --- | --- |
| Registro | POST `/auth/register` |
| Login | POST `/auth/login` |
| Perfil | GET, PUT `/auth/me` |
| Senha | PUT `/auth/password` |
| Usuários bloqueados | GET `/auth/admin/locked-users` |
| Reset admin de senha | POST `/auth/admin/users/{id}/reset-password` |
| Contas | GET, POST `/accounts` |
| Conta por Id | GET, PUT, DELETE `/accounts/{id}` |
| Categorias | GET, POST `/categories` |
| Categoria por Id | GET, PUT, DELETE `/categories/{id}` |
| Lançamentos | GET, POST `/transactions` |
| Lançamento por Id | GET, PUT, DELETE `/transactions/{id}` |
| Dashboard mensal | GET `/dashboard/monthly?year=2026&month=8` |
| Exportação CSV | GET `/exports/transactions.csv?year=2026&month=8` ou GET `/exports/transactions.csv?startDate=2026-08-01&endDate=2026-08-31` |
| Health check | GET `/health` |

## Filtros de Lançamentos

```text
GET /transactions?year=2026&month=8&type=Expense&categoryId=1&accountId=2
```

A listagem retorna `{ items, totalCount, page, pageSize }`. O padrão é `page=1` e `pageSize=50`, com limite de 100 itens por página. Páginas além do fim retornam `items: []`.

Use `search` (até 160 caracteres) para descrição, nome de conta/categoria ou data exata no formato `yyyy-MM-dd`; `sort=date|description|category|type|amount` e `direction=asc|desc` para ordenar antes da paginação. O padrão é data decrescente, com desempate por criação e identificador. Filtros e contagem respeitam o usuário autenticado.

Desde 20/10/2026, clientes que esperavam um array devem ler `items` e percorrer as páginas. A exportação CSV continua retornando todos os lançamentos dos filtros, sem paginação.


## Exportação CSV

```text
GET /exports/transactions.csv?year=2026&month=8
GET /exports/transactions.csv?startDate=2026-08-01&endDate=2026-08-31
```

Sem intervalo livre, a exportação usa o mês selecionado. Com `startDate` ou `endDate`, usa as datas informadas.

## Regras de Resposta

- `DELETE /accounts/{id}` retorna `409 Conflict` quando a conta possui lançamentos vinculados.
- `DELETE /categories/{id}` retorna `409 Conflict` quando a categoria possui lançamentos vinculados.
- `POST /transactions` e `PUT /transactions/{id}` rejeitam data anterior ao início da conta.
- `PUT /auth/me` retorna um novo JWT com os dados atualizados.
- `PUT /auth/password` valida a senha atual antes de salvar a nova senha.
- `POST /auth/login` retorna `423 Locked` quando a conta excede o limite de tentativas inválidas.
- Endpoints `/auth/admin/*` exigem usuário administrador.
- O primeiro usuário cadastrado vira administrador.
