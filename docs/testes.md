# Testes

## Resultado local informado em 24/11/2026

O usuário executou `validate-audit.ps1` fora do ambiente restrito: rebuild Docker concluído, API e SQL Server saudáveis, 87 testes .NET aprovados sem ignorados (incluindo auditoria no SQL Server, rollback e retenção) e 34 testes Angular aprovados. O relatório `out/coverage/bd5154e1-5f11-4061-a245-efa3a0853e5f/coverage.cobertura.xml` foi lido: 99,71% das linhas e 97,22% das ramificações no escopo coletado.

A navegação final inicialmente falhou antes de iniciar por ausência do Chromium gerenciado. Corrigido `check-ui.mjs` para reutilizar `CHROME_BIN`, já definido para o Chrome instalado, preservando a precedência de `PLAYWRIGHT_BROWSER_PATH`. O usuário repetiu a etapa com `-BrowserOnly`: `passed: true`, 47 capturas, `errors: []` e mensagem final PASS. API, SQL Server e frontend foram exibidos saudáveis pelo Compose. A consulta de auditoria contra API real, com banco descartável e checagens de layout mobile, passou. As validações locais planejadas da feature estão concluídas; a publicação das alterações adicionais e sua execução no CI permanecem separadas desse resultado.

As seções abaixo preservam o histórico das tentativas anteriores e dos bloqueios do ambiente do agente; o resultado atual é o desta seção.

## Ensaios adicionais preparados em 24/11/2026

- `AuditSqlServerFlowTests` reutiliza as consultas de auditoria no SQL Server e cria uma restrição real no banco descartável para rejeitar eventos. Verifica que alterações de conta e lançamento, exclusão, troca de senha e reset administrativo não persistem parcialmente quando a auditoria falha.
- O mesmo teste executa o SQL compartilhado de retenção: simulação, lote máximo de 10.000 exclusões, preservação do instante de corte e de eventos posteriores e preservação dos dados financeiros.
- O CI coleta cobertura Cobertura/XML como artefato, executa as proteções PowerShell, inclui a tela de auditoria no teste de navegador com API real e reconstrói API e frontend em Docker, validando ambos contra SQL Server descartável.
- Build local e proteções de retenção aprovados; 85 cenários passaram em execução direta, com os dois ensaios SQL condicionais não executados localmente. A tentativa de publicar a validação no GitHub foi recusada com 403 (`Resource not accessible by integration`); nenhuma branch ou PR foi criada. O rebuild no Docker Desktop local foi negado também pela política de elevação desta sessão. Esses ensaios novos ainda não têm resultado de execução no CI.
- `scripts/validation/validate-audit.ps1` reúne o rebuild, testes com SQL Server do Compose, coleta de cobertura, testes Angular e navegador com API em banco descartável. Deve ser executado em um terminal com acesso ao Docker. Não executa retenção no banco de uso: o teste de retenção usa o banco temporário do xUnit. Ao terminar, remove somente o container e o banco de navegação com identificadores únicos criados pela própria execução; a stack principal permanece ativa. Usa as portas adicionais 5083 e 4203.

## Fechamento de validação da auditoria — 24/11/2026

Verificado o [CI do commit 43ea81a](https://github.com/BernTomaz/FinTrack/actions/runs/44530544162), o mesmo HEAD local, executado em 19/11/2026. Os jobs Backend e Frontend e suas etapas de build e testes concluíram com sucesso. O backend foi executado com `FINTRACK_TEST_SQLSERVER` configurado e serviço SQL Server; o fluxo de navegador e layouts mobile também passou. Os testes Angular incluem os quatro cenários do painel de auditoria.

Essa evidência confirma os executores padrão no CI e o fluxo existente que aplica migrations e grava dados com auditoria no SQL Server. Não comprova cobertura medida, rollback provocado por falha de gravação da auditoria, consultas de auditoria no provedor SQL Server ou a execução real do script de retenção: esses casos não têm ensaio específico nesse workflow. A validação de navegador existente também não inclui a nova tela de auditoria contra API real; sua verificação específica anterior usou API simulada.

Em 24/11/2026, nova tentativa local de acesso ao Docker retornou `permission denied` no pipe `dockerDesktopLinuxEngine`. As imagens não foram reconstruídas e o estado dos containers locais não pôde ser consultado. Não houve execução de limpeza no banco de uso.

## Auditoria: etapas 1 e 2

- `AuditFlowTests` cobre snapshots de criação, edição e exclusão de lançamentos, contas e categorias; saldo inicial e data de abertura; isolamento de usuários; ausência de eventos de alteração para operações recusadas.
- Validação em 17/11/2026: build do backend e dos testes aprovado, com dois avisos xUnit2012 preexistentes em InfrastructureTests.
- O executor VSTest não conectou ao testhost (timeout de 90 segundos). Uma verificação temporária em processo único executou diretamente os métodos Fact e as linhas InlineData: 71 cenários passaram, nenhum falhou. O cenário SQL Server permaneceu excluído pela condição existente de conexão não configurada. Essa verificação não substitui a execução normal do CI nem mede cobertura.
- A atomicidade no SQL Server e a aplicação da migration não foram verificadas nesta rodada. A etapa 2 reutiliza a tabela da etapa 1.

## Auditoria: etapa 3

- `SecurityAuditFlowTests` verifica `PasswordChanged` e `AdminPasswordReset`, autor e usuário afetado, correlação, ausência de snapshots de credenciais, nova senha persistida e desbloqueio no reset administrativo.
- Requisições anônimas, usuários sem permissão, senha atual incorreta, senha nova fraca e alvo inexistente não geram eventos de sucesso.
- Validação em 18/11/2026: os dois testes falharam antes da implementação por ausência dos eventos. Após implementar, build aprovado e 73 cenários passaram em execução direta em processo único, sem falhas; o teste SQL Server ficou de fora pela condição existente de conexão não configurada.
- O VSTest continuou sem conectar ao testhost (tentativa com limite de 15 segundos). Cobertura e atomicidade no SQL Server não foram verificadas. Nenhuma configuração de testes versionada foi alterada.

## Auditoria: etapa 4

- `AuditQueryFlowTests` cobre autenticação, isolamento por proprietário (inclusive administrador autor de reset), filtros, dias UTC inclusivos, paginação, ausência de snapshots na lista, detalhes de registros excluídos, entradas inválidas e ausência de rota DELETE.
- Validação em 19/11/2026: build do backend e do frontend aprovado. Na execução direta em processo único, 85 cenários do backend passaram; a suíte normal VSTest continuou sem conectar ao testhost (15 segundos). A tentativa específica de SQL Server local com autenticação integrada retornou acesso negado, sem validar as consultas nesse provedor.
- Os quatro testes novos do componente Angular foram compilados. A suíte Vitest não executou: primeiro faltava o Chromium gerenciado; com `CHROME_BIN` apontando para o Chrome instalado, o ambiente negou acesso ao servidor HTTP local (`ERR_NETWORK_ACCESS_DENIED`).
- Verificação complementar no Chrome headless com arquivos compilados e API simulada, sem rede externa: navegação pelo menu, detalhes, texto com HTML escapado, paginação, filtro com retorno vazio, recuperação de erro, teclado e ausência de corte horizontal em 320, 768, 1024 e 1440 pixels. Revisão de capturas em tema claro e escuro encontrou e corrigiu um corte na paginação mobile.
- `scripts/validation/check-audit-retention.ps1` passou com Docker simulado, cobrindo simulação sem DELETE, limite de exclusão, corte UTC estrito, prazo inválido e nome de banco inválido. Não houve limpeza real; execução SQL da retenção, atomicidade e cobertura continuam pendentes de ambiente habilitado.

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

Os testes de fluxo da API estão divididos em `AuthFlowTests`, `TransactionFlowTests`, `DashboardFlowTests`, `FinancialValidationFlowTests`, `UserIsolationFlowTests`, `HostingFlowTests` e `SqlServerFlowTests`. O suporte compartilhado fica em `tests/FinTrack.Tests/Fixtures/`. No frontend, cada componente mantém seus testes na mesma pasta; os testes de login e cadastro ficam em `auth/auth.component.spec.ts`.

```powershell
dotnet test FinTrack.slnx -m:1
cd src/FinTrack.Web
npm test -- --watch=false --browsers=ChromiumHeadless
npm run build
```

Os testes Angular usam Vitest com Chromium pelo Playwright e verificam carregamento inicial, reaproveitamento de contas e categorias, atualização após edição e exclusão, falhas de carregamento, troca rápida de mês e limpeza dos dados ao sair. O CI executa os testes do backend e do frontend, configura SQL Server 2022 em um serviço temporário e executa o fluxo no navegador.

## Verificação com SQL Server

O teste abaixo executa o dashboard no SQL Server real, incluindo migrations, agrupamentos, saldos, limites de mês e isolamento por usuário. Também verifica paginação, busca, ordenação, exportação, recálculo após edição/exclusão, rejeição de acessos entre usuários e persistência dos limites monetários. O CI fornece essa conexão. No Windows, sem `FINTRACK_TEST_SQLSERVER` definida, o projeto carrega automaticamente `tests/FinTrack.Tests/visualstudio.runsettings` e usa `localhost` com autenticação integrada. Isso exige a instância padrão do SQL Server em execução e permissão para criar e excluir bancos. Após alterar essa configuração, recarregue o projeto no Visual Studio e execute todos os testes novamente. Nos demais sistemas, sem a variável, apenas esse teste é marcado como ignorado; os demais usam o banco em memória.

Execute a partir da raiz, usando uma instância de desenvolvimento com permissão para criar e excluir bancos:

```powershell
$env:FINTRACK_TEST_SQLSERVER = 'Server=localhost;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True'
dotnet test FinTrack.slnx -m:1 --filter Dashboard_queries_work_on_sql_server
Remove-Item Env:FINTRACK_TEST_SQLSERVER
```

O teste substitui o nome do banco da conexão por `FinTrackTests_<identificador aleatório>` e exclui somente esse banco ao terminar. Não use credenciais de produção. Se o processo for interrompido à força, o banco temporário pode precisar ser removido manualmente.

## GIFs da documentação

Com frontend e API locais em execução, use `scripts/docs/capture-readme-gifs.mjs` e depois `scripts/docs/build-readme-gifs.py` para gerar os quatro GIFs do README. A captura exige Playwright e Chrome; a montagem exige Python com Pillow. O script aceita `FINTRACK_WEB_URL`, `FINTRACK_API_URL`, `PLAYWRIGHT_PATH` e `PLAYWRIGHT_BROWSER_PATH`.

Aponte a API para um banco descartável: a captura cadastra um usuário e lançamentos fictícios de setembro de 2026, selecionando esse mês no dashboard. As imagens intermediárias ficam em `docs/assets/gif-frames/`; somente os GIFs finais precisam ser versionados. Remova o banco de demonstração após encerrar a API e conferir seu nome.

## Validação visual e de volume

O Playwright está fixado como dependência de desenvolvimento do frontend. Depois de `npm ci` em `src/FinTrack.Web`, execute `npx playwright install chromium` nessa pasta. No Linux, use `npx playwright install --with-deps chromium`. O pacote pode ser substituído por `PLAYWRIGHT_PATH` e um navegador instalado por `PLAYWRIGHT_BROWSER_PATH`.

Use uma API local separada, ligada a um banco novo `FinTrackValidation_<GUID>`, e configure `FINTRACK_API_URL` para ela. Nunca aponte esses scripts para os dados de uso pessoal.

- `node scripts/validation/check-ui.mjs`: serve o build de produção em `localhost:4201`; valida cadastro, login, contas, categorias, lançamentos, filtros, bloqueios de edição/exclusão, exportação completa, tema, páginas de relatórios, busca além da primeira página, totais do gráfico e larguras de 320 a 1440 pixels. Salva capturas e resultados em `out/validation/ui`.
- `node scripts/validation/check-database.mjs`: exige `FINTRACK_VALIDATION_DATABASE`, autenticação integrada no SQL Server local e `sqlcmd`. Confere que a API usa o banco informado, insere 120 mil lançamentos fictícios e mede sete requisições após uma de aquecimento. A listagem medida usa a página padrão de 50 itens e confere o total de 10 mil no mês; as medições antigas de listagem completa não são diretamente comparáveis. Executa também oito consultas simultâneas por endpoint, 40 requisições em cada grupo, conferindo resultados, mediana e percentil 95. Também restaura um backup em outro banco temporário e compara os dados.
- `FINTRACK_BENCHMARK_REUSE=true` reutiliza o usuário de volume existente e repete apenas a medição, sem inserir lançamentos ou fazer outro backup.

O teste de restauração exclui sua própria cópia do banco. Ao terminar a sessão, encerrar a API temporária e remover o banco de origem descartável e os arquivos de backup gerados, conferindo os nomes antes da exclusão. A concorrência medida usa um único usuário fictício em localhost; não representa muitos usuários em produção nem uma rede móvel real. A porta do servidor visual pode ser alterada com FINTRACK_WEB_PORT (padrão 4201), desde que a API permita essa origem no CORS.

Para validar o frontend servido pelo Docker, informe `FINTRACK_WEB_URL` com a URL local do container e `FINTRACK_API_URL` com a API descartável correspondente. O frontend precisa apontar para essa API, e o CORS deve permitir sua origem. A validação visual também verifica que os valores dos cartões do resumo permanecem em uma única linha.

