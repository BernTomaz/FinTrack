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

## Status

Concluída no backend.

