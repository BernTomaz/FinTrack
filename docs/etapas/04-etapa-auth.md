# 04 - Auth

## Objetivo

Permitir cadastro, login, perfil, alteração de senha e proteção dos dados do usuário.

## Tarefas

- Cadastro de usuário
- Login
- Hash de senha
- Emissão de JWT
- Identificação do usuário logado
- Proteção dos endpoints privados
- Perfil editável com e-mail somente leitura
- Alteração de senha autenticada
- Bloqueio de login após tentativas inválidas
- Primeiro usuário cadastrado como administrador
- Reset admin de senha para usuários bloqueados

## Endpoints

```text
POST /auth/register
POST /auth/login
GET /auth/me
PUT /auth/me
PUT /auth/password
GET /auth/admin/locked-users
POST /auth/admin/users/{id}/reset-password
```

## Status

Concluída no backend.

