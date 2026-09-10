# Wiki do FinTrack

Esta página organiza a documentação do projeto. A fonte oficial continua sendo a pasta `docs/` versionada junto com o código.

## Comece por aqui

- [Visão geral do MVP](etapas/00-etapa-visao-geral.md)
- [Status do projeto](status-projeto.md)
- [Roadmap](roadmap.md)
- [Configuração local](configuracao.md)

## Produto

- [Fluxo geral](fluxos/00-fluxo-geral.md)
- [Fluxo de autenticação](fluxos/01-fluxo-autenticacao.md)
- [Fluxo financeiro](fluxos/02-fluxo-financeiro.md)
- [Fluxo de dashboard e exportação](fluxos/03-fluxo-dashboard-exportacao.md)

## Técnico

- [Arquitetura](arquitetura.md)
- [Endpoints](endpoints.md)
- [Testes](testes.md)
- [Migrations](operacao/migrations.md)
- [Checklist de entrega](operacao/deploy-checklist.md)

## Etapas de implementação

- [00 - Visão geral](etapas/00-etapa-visao-geral.md)
- [01 - Estrutura do projeto](etapas/01-etapa-estrutura-projeto.md)
- [02 - Domínio](etapas/02-etapa-dominio.md)
- [03 - Banco e EF Core](etapas/03-etapa-banco-efcore.md)
- [04 - Autenticação](etapas/04-etapa-auth.md)
- [05 - Contas e categorias](etapas/05-etapa-contas-categorias.md)
- [06 - Lançamentos](etapas/06-etapa-lancamentos.md)
- [07 - Dashboard e exportação](etapas/07-etapa-dashboard-exportacao.md)
- [08 - Frontend Angular](etapas/08-etapa-frontend-angular.md)
- [09 - Testes e finalização](etapas/09-etapa-testes-finalizacao.md)

## Regras importantes

- O saldo atual considera os saldos iniciais das contas que já começaram até o fim do mês selecionado, mais receitas, menos despesas.
- Uma conta iniciada em agosto não entra no saldo de julho.
- Um lançamento não pode ter data anterior à data de início da conta.
- Contas e categorias com lançamentos vinculados não podem ser excluídas diretamente.
- O gráfico de fluxo de caixa usa apenas lançamentos reais, sem meses ou valores demonstrativos.

## Se for usar a Wiki do GitHub

Use esta página como `Home` da Wiki e mantenha links para os arquivos da pasta `docs/`. Evite copiar todo o conteúdo para não precisar atualizar dois lugares.
