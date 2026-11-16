# Roadmap

## Agora

- Auditoria por etapas: etapa 1 implementada para criação, edição e exclusão de lançamentos, com snapshots antes/depois, autor, proprietário, data UTC e correlação na tabela AuditEvents.
- Próximas etapas da auditoria: contas e categorias; alteração de senha e ações administrativas; consulta autenticada do histórico e definição de retenção.
- A etapa 1 salva evento e lançamento no mesmo SaveChanges (transação do EF Core no SQL Server). Não há endpoint para editar, excluir ou consultar eventos; alterações diretas no banco não são cobertas. Registros anteriores à implantação não possuem histórico retroativo.

- Acompanhar o CI das correções de segurança e sincronização dos testes.
- Avaliar validação em aparelho físico antes da publicação; a validação realizada foi em larguras mobile simuladas.
- Revisar deploy público.

## MVP concluído

- Usuários
- Login
- Contas
- Categorias
- Lançamentos
- Dashboard mensal
- Gráfico de fluxo de caixa com dados reais
- Exportação CSV
- Exclusão de lançamentos
- Bloqueio de exclusão de contas e categorias com lançamentos vinculados
- Saldo atual com saldos iniciais das contas
- Perfil editável
- Alteração de senha
- Testes principais
- CI de backend e frontend

## Depois do MVP

- Deploy público.
- Melhorias visuais.
- Relatórios adicionais.
- Definir rotina de backup de produção e repetir o ensaio de restauração na hospedagem escolhida.

## Pós-MVP concluído

- Duplicação de lançamentos pelo dashboard e relatórios, sem alterar o original.
- Fluxo de caixa diário com receitas, despesas e saldo acumulado do mês, consulta por dia e tabela de valores.
- Detalhamento de despesas por categoria com acesso aos lançamentos filtrados.
- Script de backup local e ensaio real de restauração em banco separado, registrados em 27/10/2026.

- Edição de contas.
- Edição de categorias.
- Paginação dos relatórios com filtros e ordenação no servidor.
- SQL Server e validação de navegador configurados no CI.
- Revisão de CSS e documentação em 20/10/2026.
- Dependências atualizadas e testes migrados para Vitest: auditorias sem alertas conhecidos.
- Medição com 120 mil lançamentos e oito consultas simultâneas.

## Fora do MVP

- Integração bancária.
- IA.
- OCR.
- App mobile.
- Multi-moeda.
- Assinatura paga.
