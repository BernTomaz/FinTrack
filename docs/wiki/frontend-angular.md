# Frontend Angular

## Visão Geral

O frontend do FinTrack é uma aplicação Angular 21 focada no fluxo principal do MVP.

## Telas

- Login
- Cadastro
- Dashboard
- Contas
- Categorias
- Lançamentos
- Relatórios
- Exportação
- Perfil
- Alteração de senha
- Administração de usuários bloqueados
- Status do sistema
- Preferências
- Sobre

## Componentes

Os painéis principais foram separados em componentes para manter o arquivo principal menor e facilitar manutenção:

- `dashboard-panel`
- `accounts-panel`
- `categories-panel`
- `transaction-form-panel`
- `reports-panel`
- `export-panel`
- `profile-panel`
- `password-panel`

As telas de administração, status, preferências e sobre ficam no componente principal porque são simples e não justificam um componente próprio agora.

## API e Sessão

O serviço Angular centraliza:

- Chamadas HTTP.
- Token JWT.
- Dados do usuário autenticado.
- URL base da API.
- Indicador de administrador.

## Responsividade

O dashboard apresenta receitas e despesas por dia, saldo acumulado do mês (sem saldos iniciais das contas), seletor de dia e tabela de valores. As despesas por categoria abrem os lançamentos filtrados. Dashboard e relatórios permitem duplicar um lançamento, revisar os campos e salvar uma nova cópia sem alterar o original.

Responsividade mobile é requisito do projeto. As telas devem continuar utilizáveis em desktop e celular.

## Configuração da API

O frontend lê a URL da API pelo metadado:

```html
<meta name="fintrack-api-url" content="http://localhost:5080">
```

Em desenvolvimento local e Docker, o valor padrão é `http://localhost:5080`.
