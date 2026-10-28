# Fluxo de dashboard e exportação

## Dashboard

```text
Usuário escolhe mês/ano
  -> API busca lançamentos do período
  -> API calcula totais e saldo atual até o fim do mês selecionado
  -> frontend exibe resumo
```

O saldo atual inclui apenas contas que já começaram no período escolhido. Uma conta iniciada em agosto não entra no saldo de julho.

## Gráfico de fluxo de caixa

```text
API agrega todos os lançamentos do usuário no mês selecionado por dia
  -> calcula receitas, despesas e saldo acumulado do mês
  -> frontend completa os dias sem movimentação mantendo o saldo anterior
  -> exibe barras diárias e linha do saldo acumulado
```

O saldo acumulado começa em zero no mês, sem incluir saldos iniciais das contas. O gráfico não depende da paginação dos relatórios nem dos cinco lançamentos recentes. O usuário pode consultar um dia ou abrir a tabela com todos os valores.

A pizza usa os totais completos por categoria, com legenda de cores, valores e percentuais. Cada categoria da legenda abre os lançamentos correspondentes nos relatórios.

## Exportação

```text
Usuário aplica filtros
  -> solicita CSV
  -> API gera arquivo
  -> navegador baixa o resultado
```

