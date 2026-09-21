# FinTrack Wiki

Esta página organiza a documentação do FinTrack no mesmo formato usado na wiki do GradeFlow: uma página inicial curta, páginas técnicas separadas e navegação por assunto.

## Visão Geral

FinTrack é um sistema web para controle financeiro pessoal, com cadastro de usuários, login, contas financeiras, categorias, lançamentos, dashboard mensal, gráfico de fluxo de caixa e exportação CSV.

## Status Atual

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

## Segurança Atual

- Cadastro e login com hash de senha.
- JWT para proteger endpoints do usuário autenticado.
- E-mail do perfil somente leitura.
- Alteração de senha exige a senha atual.
- Dados financeiros isolados por usuário autenticado.
- `.env` real fora do versionamento.

## Páginas

Use os arquivos de `docs/wiki/` como base para publicar na Wiki do GitHub:

- [Home](wiki/Home.md)
- [Sidebar](wiki/_Sidebar.md)
- [Footer](wiki/_Footer.md)
- [Arquitetura](wiki/arquitetura.md)
- [Domínio](wiki/dominio.md)
- [API](wiki/api.md)
- [Autenticação e Autorização](wiki/autenticacao-autorizacao.md)
- [Frontend Angular](wiki/frontend-angular.md)
- [Banco de Dados](wiki/banco-de-dados.md)
- [Testes](wiki/testes.md)
- [CI/CD](wiki/cicd.md)
- [Docker](wiki/docker.md)
- [Deploy](wiki/deploy.md)
- [Roadmap](wiki/roadmap.md)
- [Decisões Técnicas](wiki/decisoes-tecnicas.md)
- [Contribuição](wiki/contribuicao.md)
- [Glossário](wiki/glossario.md)

## Regras Importantes

- O saldo atual considera saldos iniciais das contas que já começaram até o fim do mês selecionado, mais receitas, menos despesas.
- Uma conta iniciada em agosto não entra no saldo de julho.
- Um lançamento não pode ter data anterior à data de início da conta.
- Contas e categorias com lançamentos vinculados não podem ser excluídas diretamente.
- O gráfico de fluxo de caixa usa apenas lançamentos reais.
