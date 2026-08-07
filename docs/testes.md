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

## Estratégia

Começar por testes de regra de negócio e services principais. Testes de controller entram quando a autenticação e os endpoints estiverem estáveis.

