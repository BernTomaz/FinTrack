# Testes

## Resultado confirmado em 24/11/2026

Validação local concluída: rebuild Docker, 87 testes .NET aprovados sem ignorados, build Angular e 34 testes frontend aprovados. O navegador com API real e banco descartável passou com 47 capturas e nenhum erro, incluindo o histórico de atividades e larguras mobile simuladas. API, SQL Server e frontend estavam saudáveis no Compose. Não houve teste em aparelho físico.

O relatório `out/coverage/bd5154e1-5f11-4061-a245-efa3a0853e5f/coverage.cobertura.xml` registrou 99,71% das linhas e 97,22% das ramificações no escopo coletado, definido em `tests/FinTrack.Tests/coverlet.runsettings`. Os percentuais não abrangem todos os arquivos do repositório.

A última execução remota conferida foi o [CI do commit 43ea81a, de 19/11/2026](https://github.com/BernTomaz/FinTrack/actions/runs/44530544162). Ela antecede os testes adicionais de rollback, retenção e cobertura. O resultado local não confirma o CI de commits posteriores. Revisão documental em 25/11/2026, sem nova execução das suítes.

## Validação completa da auditoria

Na raiz do projeto, com Docker Desktop disponível, .NET 10, Node, PowerShell e dependências do frontend instaladas:

```powershell
.\scripts\validation\validate-audit.ps1
```

O script reconstrói a stack, executa testes com SQL Server, coleta cobertura, verifica as proteções de retenção, compila e testa o Angular e valida o navegador com API real. Os testes de integração e navegação usam bancos descartáveis; não aplicam retenção ao banco de uso. A stack principal permanece ativa. As portas adicionais 5083 e 4203 devem estar disponíveis. Relatórios ficam em `out/coverage` e capturas em `out/validation/ui`.

Para repetir somente o navegador:

```powershell
.\scripts\validation\validate-audit.ps1 -BrowserOnly
```

Essa opção reutiliza imagens e build existentes; não valida alterações ainda não compiladas. O navegador usa `PLAYWRIGHT_BROWSER_PATH`, depois `CHROME_BIN` e, sem ambos, o Chromium gerenciado pelo Playwright. Para instalar este último, execute `npx playwright install chromium` em `src/FinTrack.Web`.

## Cobertura funcional da auditoria

- `AuditFlowTests`: criação, edição e exclusão de contas, categorias e lançamentos, snapshots, isolamento e operações recusadas.
- `SecurityAuditFlowTests`: troca de senha e reset administrativo, autor, proprietário e ausência de snapshots de credenciais.
- `AuditQueryFlowTests`: autenticação, isolamento inclusive para administradores, filtros, datas UTC, paginação, detalhes e entradas inválidas.
- `AuditSqlServerFlowTests`: consultas no SQL Server, rollback quando a auditoria falha e SQL de retenção em banco temporário. Verifica simulação, limite de 10.000 exclusões, corte estrito e preservação dos dados financeiros.
- `audit-panel.component.spec.ts` e `check-ui.mjs`: comportamento do painel e navegação com API real.
- `check-audit-retention.ps1`: proteções do comando PowerShell com Docker simulado, sem banco.

A retenção manual está documentada em [Operação do banco](../scripts/database/README.md).

## Ferramentas

- xUnit
- Microsoft.AspNetCore.Mvc.Testing

## Testes do MVP

- Criar conta com dados válidos
- Impedir transação com valor menor ou igual a zero
- Impedir usuário de usar conta de outro usuário
- Impedir usuário de usar categoria de outro usuário
- Calcular total de receitas do mês
- Calcular total de despesas do mês
- Calcular saldo mensal
- Agrupar receitas e despesas por dia e calcular o saldo acumulado do mês sem incluir os saldos iniciais das contas (`DailyCashFlowTests`)
- Exportar CSV com lançamentos filtrados por mês ou intervalo
- Impedir exclusão de conta com lançamentos vinculados
- Impedir exclusão de categoria com lançamentos vinculados
- Permitir exclusão de lançamentos pela API
- Validar nome, e-mail e senha no cadastro e login
- Validar nome de conta e categoria
- Validar descrição máxima de lançamento
- Validar que o saldo inicial da conta respeita a data de início
- Impedir lançamento com data anterior ao início da conta
- Recalcular saldos exatos após editar valor, tipo e mês de um lançamento e depois excluí-lo
- Negar leitura, edição, exclusão e uso de identificadores de outro usuário, sem alterar os dados existentes
- Rejeitar tipos inexistentes e valores fora da precisão monetária, preservando os limites válidos

## Estratégia

Os testes cobrem regras de negócio e fluxos principais da API, incluindo autenticação, isolamento por usuário, validações, dashboard, exportação CSV e regras de exclusão.

As regressões também verificam alterações de contas e categorias com lançamentos existentes, campos obrigatórios nulos, o último dia do ano 9999 e saldos históricos em meses vazios e anos bissextos.

## Execução

Os testes de fluxo da API estão divididos em `AuthFlowTests`, `TransactionFlowTests`, `DashboardFlowTests`, `FinancialValidationFlowTests`, `UserIsolationFlowTests`, `HostingFlowTests`, `SqlServerFlowTests` e as classes de auditoria descritas acima. O suporte compartilhado fica em `tests/FinTrack.Tests/Fixtures/`. No frontend, cada componente mantém seus testes na mesma pasta; os testes de login e cadastro ficam em `auth/auth.component.spec.ts`.

```powershell
dotnet test FinTrack.slnx -m:1
cd src/FinTrack.Web
npm test -- --watch=false --browsers=ChromiumHeadless
npm run build
```

Os testes Angular usam Vitest com Chromium pelo Playwright e verificam carregamento inicial, reaproveitamento de contas e categorias, atualização após edição e exclusão, falhas de carregamento, troca rápida de mês e limpeza dos dados ao sair. O CI executa os testes do backend e do frontend, configura SQL Server 2022 em um serviço temporário e executa o fluxo no navegador.

## Verificação com SQL Server

`SqlServerFlowTests` executa o dashboard no SQL Server real, incluindo migrations, agrupamentos, saldos, limites de mês e isolamento por usuário. Também verifica paginação, busca, ordenação, exportação, recálculo após edição/exclusão, rejeição de acessos entre usuários e persistência dos limites monetários. O CI fornece essa conexão. No Windows, sem `FINTRACK_TEST_SQLSERVER` definida, o projeto carrega automaticamente `tests/FinTrack.Tests/visualstudio.runsettings` e usa `localhost` com autenticação integrada. Isso exige a instância padrão do SQL Server em execução e permissão para criar e excluir bancos. Após alterar essa configuração, recarregue o projeto no Visual Studio e execute todos os testes novamente. Nos demais sistemas, sem a variável, os dois testes condicionais de SQL Server são ignorados; os demais usam o banco em memória. `AuditSqlServerFlowTests` também exige permissão para criar e excluir bancos.

Execute a partir da raiz, usando uma instância de desenvolvimento com permissão para criar e excluir bancos:

```powershell
$env:FINTRACK_TEST_SQLSERVER = 'Server=localhost;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True'
dotnet test FinTrack.slnx -m:1 --filter "FullyQualifiedName~SqlServerFlowTests"
Remove-Item Env:FINTRACK_TEST_SQLSERVER
```

Os testes usam bancos únicos `FinTrackTests_<identificador>` ou `FinTrackAuditTests_<identificador>` e excluem somente os bancos criados por eles ao terminar. Não use credenciais de produção. Se o processo for interrompido à força, o banco temporário pode precisar ser removido manualmente.

## GIFs da documentação

Com frontend e API locais em execução, use `scripts/docs/capture-readme-gifs.mjs` e depois `scripts/docs/build-readme-gifs.py` para gerar os quatro GIFs do README. A captura exige Playwright e Chrome; a montagem exige Python com Pillow. O script aceita `FINTRACK_WEB_URL`, `FINTRACK_API_URL`, `PLAYWRIGHT_PATH` e `PLAYWRIGHT_BROWSER_PATH`.

Aponte a API para um banco descartável: a captura cadastra um usuário e lançamentos fictícios de setembro de 2026, selecionando esse mês no dashboard. As imagens intermediárias ficam em `docs/assets/gif-frames/`; somente os GIFs finais precisam ser versionados. Remova o banco de demonstração após encerrar a API e conferir seu nome.

## Validação visual e de volume

O Playwright está fixado como dependência de desenvolvimento do frontend. Depois de `npm ci` em `src/FinTrack.Web`, execute `npx playwright install chromium` nessa pasta. No Linux, use `npx playwright install --with-deps chromium`. O pacote pode ser substituído por `PLAYWRIGHT_PATH` e um navegador instalado por `PLAYWRIGHT_BROWSER_PATH`.

Use uma API local separada, ligada a um banco novo `FinTrackValidation_<GUID>`, e configure `FINTRACK_API_URL` para ela. Nunca aponte esses scripts para os dados de uso pessoal.

- `node scripts/validation/check-ui.mjs`: serve o build de produção em `localhost:4201`; valida cadastro, login, contas, categorias, lançamentos, filtros, bloqueios de edição/exclusão, exportação completa, histórico de atividades, tema, páginas de relatórios, busca além da primeira página, totais do gráfico e larguras de 320 a 1440 pixels. Salva capturas e resultados em `out/validation/ui`.
- `node scripts/validation/check-database.mjs`: exige `FINTRACK_VALIDATION_DATABASE`, autenticação integrada no SQL Server local e `sqlcmd`. Confere que a API usa o banco informado, insere 120 mil lançamentos fictícios e mede sete requisições após uma de aquecimento. A listagem medida usa a página padrão de 50 itens e confere o total de 10 mil no mês; as medições antigas de listagem completa não são diretamente comparáveis. Executa também oito consultas simultâneas por endpoint, 40 requisições em cada grupo, conferindo resultados, mediana e percentil 95. Também restaura um backup em outro banco temporário e compara os dados.
- `FINTRACK_BENCHMARK_REUSE=true` reutiliza o usuário de volume existente e repete apenas a medição, sem inserir lançamentos ou fazer outro backup.

O teste de restauração exclui sua própria cópia do banco. Ao terminar a sessão, encerrar a API temporária e remover o banco de origem descartável e os arquivos de backup gerados, conferindo os nomes antes da exclusão. A concorrência medida usa um único usuário fictício em localhost; não representa muitos usuários em produção nem uma rede móvel real. A porta do servidor visual pode ser alterada com FINTRACK_WEB_PORT (padrão 4201), desde que a API permita essa origem no CORS.

Para validar o frontend servido pelo Docker, informe `FINTRACK_WEB_URL` com a URL local do container e `FINTRACK_API_URL` com a API descartável correspondente. O frontend precisa apontar para essa API, e o CORS deve permitir sua origem. A validação visual também verifica que os valores dos cartões do resumo permanecem em uma única linha.

