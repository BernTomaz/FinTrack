# Frontend Angular

## Visão Geral

O frontend do FinTrack é uma aplicação Angular 20 focada no fluxo principal do MVP.

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

## API e Sessão

O serviço Angular centraliza:

- Chamadas HTTP.
- Token JWT.
- Dados do usuário autenticado.
- URL base da API.

## Responsividade

Responsividade mobile é requisito do projeto. As telas devem continuar utilizáveis em desktop e celular.

## Configuração da API

O frontend lê a URL da API pelo metadado:

```html
<meta name="fintrack-api-url" content="http://localhost:5080">
```

Em desenvolvimento local e Docker, o valor padrão é `http://localhost:5080`.
