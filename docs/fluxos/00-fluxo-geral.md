# Fluxo geral

```text
Cadastro/Login
  -> Dashboard
  -> Contas
  -> Categorias
  -> Lançamentos
  -> Filtros
  -> Exportação CSV
  -> Perfil/Preferências
```

## Jornada principal

1. Usuário cria cadastro.
2. Usuário faz login.
3. Usuário cadastra uma ou mais contas.
4. Usuário cadastra categorias de receita e despesa.
5. Usuário registra receitas e despesas.
6. Dashboard mostra resumo mensal.
7. Usuário filtra lançamentos.
8. Usuário exporta CSV quando precisar.
9. Usuário edita perfil, altera senha ou ajusta preferências locais.

Fluxos de suporte:

- Após tentativas inválidas, login bloqueia a conta.
- Administrador pode redefinir senha temporária de usuários bloqueados.
- Status do sistema mostra saúde da API e do banco.

