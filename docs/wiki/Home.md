# FinTrack Wiki

## Visão geral

FinTrack é um sistema web para controle financeiro pessoal, com cadastro de usuários, login, contas financeiras, categorias, lançamentos, dashboard mensal, gráfico de fluxo de caixa e exportação CSV.

## Status atual

O MVP está funcional e validado localmente com backend, frontend, banco, Docker, testes automatizados e CI.

Próximas etapas:

1. Decidir o deploy público.
2. Ajustar melhorias pequenas pós-MVP.
3. Manter recursos maiores documentados para depois.

## Objetivos

- Demonstrar conhecimentos em .NET, Angular e SQL Server.
- Aplicar arquitetura em camadas sem complexidade desnecessária.
- Controlar receitas, despesas, contas e categorias de forma simples.
- Exibir uma visão mensal clara do fluxo financeiro.
- Servir como projeto de portfólio focado em um MVP realista.

## Tecnologias

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication

### Frontend

- Angular 20
- TypeScript
- Reactive Forms
- HttpClient
- Layout responsivo mobile-first

### DevOps

- GitHub Actions
- Docker
- Docker Compose
- Health checks
- Migrations EF Core

## Segurança atual

- Cadastro e login com hash de senha.
- JWT para proteger endpoints do usuário autenticado.
- E-mail do perfil somente leitura.
- Alteração de senha exige a senha atual.
- Dados financeiros isolados por usuário autenticado.
- `.env` real fora do versionamento.

## Páginas

- [Arquitetura](arquitetura)
- [Domínio](dominio)
- [API](api)
- [Autenticação e autorização](autenticacao-autorizacao)
- [Frontend Angular](frontend-angular)
- [Banco de dados](banco-de-dados)
- [Testes](testes)
- [CI/CD](cicd)
- [Docker](docker)
- [Deploy](deploy)
- [Roadmap](roadmap)
- [Decisões técnicas](decisoes-tecnicas)
- [Contribuição](contribuicao)
- [Glossário](glossario)
