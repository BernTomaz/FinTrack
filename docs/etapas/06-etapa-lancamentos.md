# 06 - Lançamentos

## Objetivo

Registrar receitas e despesas.

## Tarefas

- Criar lançamento
- Editar lançamento
- Excluir lançamento
- Listar lançamentos
- Filtrar por mês, ano, tipo, categoria e conta

## Regras

- Valor deve ser maior que zero
- Data é obrigatória
- Conta e categoria devem pertencer ao usuário logado
- Tipo do lançamento deve combinar com a categoria

## Endpoints

```text
GET /transactions
GET /transactions/{id}
POST /transactions
PUT /transactions/{id}
DELETE /transactions/{id}
```

Filtros:

```text
GET /transactions?year=2026&month=8&type=Expense&categoryId={id}&accountId={id}
```

A listagem retorna `{ items, totalCount, page, pageSize }`. O padrão é `page=1` e `pageSize=50`, com limite de 100 itens por página. Páginas além do fim retornam `items: []`.

Use `search` (até 160 caracteres) para descrição, nome de conta/categoria ou data exata no formato `yyyy-MM-dd`; `sort=date|description|category|type|amount` e `direction=asc|desc` para ordenar antes da paginação. O padrão é data decrescente, com desempate por criação e identificador. Filtros e contagem respeitam o usuário autenticado.

Desde 20/10/2026, clientes que esperavam um array devem ler `items` e percorrer as páginas. A exportação CSV continua retornando todos os lançamentos dos filtros, sem paginação.


## Status

Concluída no backend.

