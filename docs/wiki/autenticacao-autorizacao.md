# Autenticação e autorização

## Visão geral

O FinTrack usa autenticação JWT para proteger os dados financeiros do usuário autenticado.

## Fluxo

1. Usuário cria uma conta em `/auth/register`.
2. Usuário faz login em `/auth/login`.
3. A API retorna um token JWT.
4. O frontend envia o token nas chamadas protegidas.
5. A API identifica o usuário e filtra os dados pelo dono autenticado.

## Perfil

O usuário pode:

- Consultar o perfil em `/auth/me`.
- Atualizar o nome em `/auth/me`.
- Alterar a senha em `/auth/password`.

O e-mail fica somente leitura depois do cadastro.

## Isolamento de dados

Contas, categorias, lançamentos, dashboard e exportação CSV sempre usam o usuário autenticado como filtro.

## Segurança atual

- Senhas salvas como hash.
- JWT assinado pela chave configurada no ambiente.
- Chave JWT e senhas locais fora do versionamento.
- Alteração de senha exige a senha atual.
