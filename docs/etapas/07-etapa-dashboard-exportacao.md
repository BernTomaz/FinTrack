# 07 - Dashboard e exportação

## Dashboard mensal

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
GET /exports/transactions.csv?startDate=2026-08-01&endDate=2026-08-31
```

## Endpoints

```text
GET /dashboard/monthly?year=2026&month=8
GET /exports/transactions.csv?year=2026&month=8
GET /exports/transactions.csv?startDate=2026-08-01&endDate=2026-08-31
```

## Status

Concluída no backend.

