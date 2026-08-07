# Testes

## Ferramentas

- xUnit
- FluentAssertions

## Testes Minimos do MVP

- Criar conta com dados validos
- Impedir transacao com valor menor ou igual a zero
- Impedir usuario de usar conta de outro usuario
- Impedir usuario de usar categoria de outro usuario
- Calcular total de receitas do mes
- Calcular total de despesas do mes
- Calcular saldo mensal
- Exportar CSV com lancamentos filtrados

## Estrategia

Comecar por testes de regra de negocio e services principais. Testes de controller entram quando a autenticacao e os endpoints estiverem estaveis.

