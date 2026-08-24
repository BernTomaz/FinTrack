# Testes

## Ferramentas

- xUnit
- FluentAssertions

## Testes Mínimos do MVP

- Criar conta com dados válidos
- Impedir transação com valor menor ou igual a zero
- Impedir usuário de usar conta de outro usuário
- Impedir usuário de usar categoria de outro usuário
- Calcular total de receitas do mês
- Calcular total de despesas do mês
- Calcular saldo mensal
- Exportar CSV com lançamentos filtrados
- Impedir exclusão de conta com lançamentos vinculados
- Impedir exclusão de categoria com lançamentos vinculados
- Permitir exclusão de lançamentos pela API
- Validar nome, e-mail e senha no cadastro e login
- Validar nome de conta e categoria
- Validar descrição máxima de lançamento

## Estratégia

Começar por testes de regra de negócio e fluxos principais da API. Os testes de endpoints devem cobrir autenticação, isolamento por usuário, validações, dashboard, exportação CSV e regras de exclusão.

