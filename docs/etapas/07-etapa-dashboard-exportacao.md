# 07 - Dashboard e Exportação

## Dashboard Mensal

Mostrar:

- Total de receitas do mês
- Total de despesas do mês
- Saldo do mês
- Saldo geral atual
- Gastos por categoria
- Últimos lançamentos

## Exportação CSV

Permitir baixar lançamentos filtrados por período.

Endpoint:

```text
GET /exports/transactions.csv?year=2026&month=8
```

## Endpoints

```text
GET /dashboard/monthly?year=2026&month=8
GET /exports/transactions.csv?year=2026&month=8
```

## Status

Concluída no backend.

