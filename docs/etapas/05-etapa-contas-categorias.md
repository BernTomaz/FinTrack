# 05 - Contas e Categorias

## Objetivo

Permitir que o usuário organize onde o dinheiro entra/sai e como os gastos serão classificados.

## Contas

- Listar
- Criar
- Editar
- Excluir

## Categorias

- Listar
- Criar
- Editar
- Excluir

## Regras

- Conta pertence a um usuário
- Categoria pertence a um usuário
- Usuário não acessa dados de outro usuário

## Endpoints

```text
GET /accounts
GET /accounts/{id}
POST /accounts
PUT /accounts/{id}
DELETE /accounts/{id}

GET /categories
GET /categories/{id}
POST /categories
PUT /categories/{id}
DELETE /categories/{id}
```

## Status

Concluída no backend.

