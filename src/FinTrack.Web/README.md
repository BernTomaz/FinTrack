# FinTrack Web

Frontend Angular do FinTrack.

## Funcionalidades

- Cadastro e login de usuários
- Dashboard mensal
- Fluxo de caixa diário com saldo acumulado do mês, consulta por dia e tabela de valores
- Despesas por categoria com acesso aos lançamentos filtrados
- Duplicação de lançamentos com revisão antes de salvar
- Gestão de contas
- Gestão de categorias
- Gestão de lançamentos
- Exportação CSV via API
- Layout responsivo para desktop e mobile

## Execução local

Instalar dependências:

```bash
npm install
```

Executar em desenvolvimento:

```bash
npm start
```

Endereço local:

```text
http://localhost:4200
```

## Build

```bash
npm run build
```

O frontend espera a API do FinTrack disponível em `http://localhost:5080`.
