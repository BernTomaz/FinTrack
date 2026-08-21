# Fluxo de Dashboard e Exportação

## Dashboard

```text
Usuário escolhe mês/ano
  -> API busca lançamentos do período
  -> API calcula totais
  -> frontend exibe resumo
```

## Gráfico de fluxo de caixa

```text
Frontend recebe os lançamentos carregados
  -> agrupa os lançamentos por mês
  -> soma receitas e despesas de cada mês
  -> exibe apenas os meses com lançamentos reais
```

O gráfico não deve usar meses demonstrativos ou valores fixos. Se o usuário começou a lançar dados em agosto, o gráfico deve começar em agosto.

## Exportação

```text
Usuário aplica filtros
  -> solicita CSV
  -> API gera arquivo
  -> navegador baixa o resultado
```

