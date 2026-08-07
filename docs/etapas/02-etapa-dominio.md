# 02 - Domínio

## Objetivo

Modelar as entidades centrais do MVP.

## Entidades

- User
- Account
- Category
- Transaction

## Enums

- AccountType
- TransactionType
- CategoryType

## Regras Iniciais

- Email de usuário deve ser único
- Senha deve ser salva como hash
- Valor de lançamento deve ser maior que zero
- Usuário só acessa os próprios dados
- Tipo do lançamento deve combinar com o tipo da categoria

