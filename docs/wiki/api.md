# API

## Visão geral

A API expõe endpoints REST para autenticação, perfil, contas, categorias, lançamentos, dashboard e exportação CSV.

## Endpoints principais

| Recurso | Endpoints |
| --- | --- |
| Registro | POST `/auth/register` |
| Login | POST `/auth/login` |
| Perfil | GET, PUT `/auth/me` |
| Senha | PUT `/auth/password` |
| Contas | GET, POST `/accounts` |
| Conta por Id | GET, PUT, DELETE `/accounts/{id}` |
| Categorias | GET, POST `/categories` |
| Categoria por Id | GET, PUT, DELETE `/categories/{id}` |
| Lançamentos | GET, POST `/transactions` |
| Lançamento por Id | GET, PUT, DELETE `/transactions/{id}` |
| Dashboard mensal | GET `/dashboard/monthly?year=2026&month=8` |
| Exportação CSV | GET `/exports/transactions.csv?year=2026&month=8` |
| Health check | GET `/health` |

## Filtros de lançamentos

```text
GET /transactions?year=2026&month=8&type=Expense&categoryId=1&accountId=2
```

## Regras de resposta

- `DELETE /accounts/{id}` retorna `409 Conflict` quando a conta possui lançamentos vinculados.
- `DELETE /categories/{id}` retorna `409 Conflict` quando a categoria possui lançamentos vinculados.
- `POST /transactions` e `PUT /transactions/{id}` rejeitam data anterior ao início da conta.
- `PUT /auth/me` retorna um novo JWT com os dados atualizados.
- `PUT /auth/password` valida a senha atual antes de salvar a nova senha.
