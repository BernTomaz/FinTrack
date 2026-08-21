# Status do Projeto

## Status Atual

MVP funcional validado localmente.

## Concluído

- Documento base `FinTrack.md`
- Estrutura de pastas
- README inicial
- Documentação inicial de arquitetura, fluxos, etapas, endpoints, testes e roadmap
- Solution `FinTrack.slnx`
- Projetos .NET iniciais
- Referências entre projetos
- Endpoint `/health`
- Swagger UI em desenvolvimento
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
- Checklist final de entrega

## Em Aberto

- Preparar commit final do MVP.
- Decidir se o próximo passo será deploy público ou melhorias pós-MVP.

## Última Validação

- Data: 21/08/2026.
- Ambiente: Docker Compose com SQL Server, API e frontend.
- Usuário de teste: `mvp-fechamento-20260821130648@fintrack.local`.
- Fluxo validado: cadastro, conta, categoria de receita, categoria de despesa, receita, despesa, dashboard mensal, gráfico de fluxo de caixa com dados reais, exportação CSV e regra de bloqueio para exclusão de conta com lançamentos.
- Resultado: aprovado.
