# Domínio

## User

Representa um usuário autenticado.

Responsabilidades:

- Identificar o dono dos dados financeiros.
- Armazenar nome, e-mail e hash de senha.
- Isolar contas, categorias e lançamentos por usuário.

## Account

Representa uma conta financeira.

Responsabilidades:

- Guardar nome, tipo, saldo inicial e data de início.
- Agrupar lançamentos de receita e despesa.
- Participar do cálculo de saldo atual.

Regras:

- A data de início define quando o saldo inicial passa a contar no dashboard.
- Uma conta com lançamentos vinculados não pode ser excluída diretamente.

## Category

Representa uma categoria financeira.

Responsabilidades:

- Classificar receitas e despesas.
- Permitir análise por categoria no dashboard.

Regras:

- A categoria pertence a um usuário.
- Uma categoria com lançamentos vinculados não pode ser excluída diretamente.

## Transaction

Representa um lançamento financeiro.

Tipos suportados:

- Receita
- Despesa

Regras:

- O lançamento pertence a uma conta e a uma categoria.
- A data do lançamento não pode ser anterior à data de início da conta.
- A listagem pode ser filtrada por ano, mês, tipo, conta e categoria.

## Dashboard mensal

Consolida os dados de um mês selecionado:

- Receitas
- Despesas
- Saldo do mês
- Saldo atual até o fim do mês
- Gastos por categoria
- Últimos lançamentos
- Fluxo de caixa com dados reais
