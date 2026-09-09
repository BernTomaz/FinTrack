# Fluxo Financeiro

## Preparação

```text
Criar contas
Criar categorias
```

Ao criar uma conta, o usuário informa a data de início. O saldo inicial da conta só entra nos cálculos a partir desse período.

## Lançamento

```text
Selecionar conta
Selecionar categoria
Informar tipo
Informar valor
Informar data
Salvar
```

O sistema valida conta, categoria, valor, data e descrição antes de salvar. A categoria precisa ser do mesmo tipo do lançamento: receita com categoria de receita e despesa com categoria de despesa. A data do lançamento não pode ser anterior à data de início da conta.

## Resultado

O lançamento atualiza os cálculos de saldo, dashboard e exportação.

## Exclusão

```text
Excluir lançamento
Atualizar dashboard
Excluir conta ou categoria, se não houver mais lançamentos vinculados
```

Contas e categorias usadas em lançamentos não podem ser excluídas diretamente. Essa regra evita que lançamentos fiquem sem conta ou sem categoria.

Para remover uma conta ou categoria já utilizada:

```text
Abrir "Ver todas" em transações
Excluir os lançamentos relacionados
Abrir contas ou categorias
Excluir a conta ou categoria desejada
```

