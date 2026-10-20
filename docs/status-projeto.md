# Status do projeto

## Status atual

MVP funcional validado localmente.

## Concluído

- Documento base `FinTrack.md`
- Estrutura de pastas
- README inicial
- Documentação inicial de arquitetura, fluxos, etapas, endpoints, testes e roadmap
- Solução `FinTrack.slnx`
- Projetos .NET iniciais
- Referências entre projetos
- Endpoint `/health`
- Swagger UI em ambiente de desenvolvimento
- Docker Compose inicial para SQL Server
- Projeto Angular 20 inicial
- Tela inicial responsiva do FinTrack
- Entidades centrais do domínio
- Enums do domínio
- Testes iniciais de regras do domínio
- `FinTrackDbContext`
- Mapeamento EF Core das entidades
- Migration inicial
- Script SQL idempotente para migrations
- Suporte documentado para SQL Server via Docker, SQL Server local e LocalDB
- Cadastro e login de usuário
- Hash de senha
- Emissão de JWT
- Endpoint protegido `/auth/me`
- CRUD de contas protegido por usuário
- CRUD de categorias protegido por usuário
- CRUD de lançamentos protegido por usuário
- Filtros de lançamentos por ano, mês, tipo, conta e categoria
- Dashboard mensal com totais, saldo, gastos por categoria e últimos lançamentos
- Exportação CSV de lançamentos
- Primeira versão funcional do frontend Angular
- Telas de login, cadastro, dashboard, contas, categorias e lançamentos
- Validação ponta a ponta com API, banco e frontend via Docker
- Revisão visual básica em desktop e mobile
- Correção da integração de enums entre Angular e API
- Bloqueio de exclusão de contas e categorias com lançamentos vinculados
- Exclusão de lançamentos pela interface
- Mensagens de erro temporárias com saída suave
- Gráfico de fluxo de caixa baseado em lançamentos reais
- Validações de cadastro, login, contas, categorias e lançamentos na interface e na API
- Checklist final de entrega
- README específico do frontend
- Workflow de CI com validação de backend e frontend
- Confirmação visual antes de excluir contas, categorias e lançamentos
- Saldo atual considerando saldos iniciais apenas a partir da data de início das contas
- Bloqueio de lançamentos com data anterior ao início da conta
- Perfil editável com e-mail somente leitura
- Alteração de senha autenticada
- Serviço Angular centralizando chamadas à API e sessão
- Componentes Angular separados para dashboard, relatórios, contas, categorias, exportação, lançamentos, perfil e alteração de senha
- Edição de contas pela interface
- Edição de categorias pela interface
- Health checks no Docker Compose para SQL Server, API e frontend
- Imagens Docker locais nomeadas para API e frontend
- Script de seed/demo por API
- Feedback de carregamento no frontend
- Limite de descrição de lançamento alinhado entre API e banco
- Bloqueio de login por tentativas inválidas
- Primeiro usuário cadastrado como administrador
- Tela admin para redefinir senha de usuários bloqueados
- Status do sistema no frontend
- Preferências locais de tema, mês, cursor e pet
- Tela Sobre com autoria e link do GitHub

- Paginação de lançamentos na API e nos relatórios, com filtros e ordenação no servidor
- SQL Server real configurado no CI
- Fluxo no navegador e verificação de larguras mobile configurados no CI

## Em aberto

- Preencher manualmente o painel About do GitHub.
- Decidir se o próximo passo será deploy público ou novas melhorias pós-MVP.

## Última validação Docker

- Data: 20/10/2026.
- Comando: `docker compose up --build -d`.
- Serviços validados: SQL Server, API e frontend.
- API: health check em `/health` retornou `Healthy`.
- Frontend: `http://localhost:4201` retornou `200`.
- Fluxo real via API: cadastro, conta, categoria, lançamento, dashboard e CSV.
- Resultado: aprovado.

## Última validação automatizada

- Data: 20/10/2026.
- Backend: 54 testes aprovados, nenhum ignorado, incluindo SQL Server real e paginação.
- Frontend: 14 testes aprovados com Vitest e Chromium sem interface.
- Build de produção aprovado sem aviso de CSS; regras responsivas em arquivo próprio e declarações redundantes removidas.
- Auditorias npm e NuGet sem vulnerabilidades conhecidas. CI: corrigida a corrida na navegação encontrada na execução hospedada; nova execução em acompanhamento.

## Última validação ponta a ponta

- Data: 20/10/2026.
- Ambiente: API local, SQL Server com banco descartável e frontend com build de produção.
- Fluxo: cadastro, login, contas, categorias, receitas, despesas, edição, filtros, dashboard e CSV completo.
- Paginação: 55 lançamentos em duas páginas, busca fora da primeira página e totais independentes da página.
- 42 capturas com verificações de largura entre 320 e 1440 pixels, incluindo tema escuro e menu compacto.
- Detalhes e pendências em [Validação de 20/10/2026](operacao/validacao-2026-10-20.md).

## Fechamento das pendências

- [Resultados de segurança, volume e concorrência](operacao/fechamento-2026-10-20.md).
- Validação em aparelho físico continua dependendo de um dispositivo real.
