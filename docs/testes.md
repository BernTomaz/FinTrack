# Testes

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

## Validação visual e de volume

O Playwright está fixado como dependência de desenvolvimento do frontend. Depois de `npm ci` em `src/FinTrack.Web`, execute `npx playwright install chromium` nessa pasta. No Linux, use `npx playwright install --with-deps chromium`. O pacote pode ser substituído por `PLAYWRIGHT_PATH` e um navegador instalado por `PLAYWRIGHT_BROWSER_PATH`.

Use uma API local separada, ligada a um banco novo `FinTrackValidation_<GUID>`, e configure `FINTRACK_API_URL` para ela. Nunca aponte esses scripts para os dados de uso pessoal.

- `node scripts/validation/check-ui.mjs`: serve o build de produção em `localhost:4201`; valida cadastro, login, contas, categorias, lançamentos, filtros, bloqueios de edição/exclusão, exportação completa, tema, páginas de relatórios, busca além da primeira página, totais do gráfico e larguras de 320 a 1440 pixels. Salva capturas e resultados em `out/validation/ui`.
- `node scripts/validation/check-database.mjs`: exige `FINTRACK_VALIDATION_DATABASE`, autenticação integrada no SQL Server local e `sqlcmd`. Confere que a API usa o banco informado, insere 120 mil lançamentos fictícios e mede sete requisições após uma de aquecimento. A listagem medida usa a página padrão de 50 itens e confere o total de 10 mil no mês; as medições antigas de listagem completa não são diretamente comparáveis. Executa também oito consultas simultâneas por endpoint, 40 requisições em cada grupo, conferindo resultados, mediana e percentil 95. Também restaura um backup em outro banco temporário e compara os dados.
- `FINTRACK_BENCHMARK_REUSE=true` reutiliza o usuário de volume existente e repete apenas a medição, sem inserir lançamentos ou fazer outro backup.

O teste de restauração exclui sua própria cópia do banco. Ao terminar a sessão, encerrar a API temporária e remover o banco de origem descartável e os arquivos de backup gerados, conferindo os nomes antes da exclusão. A concorrência medida usa um único usuário fictício em localhost; não representa muitos usuários em produção nem uma rede móvel real. A porta do servidor visual pode ser alterada com FINTRACK_WEB_PORT (padrão 4201), desde que a API permita essa origem no CORS.

Para validar o frontend servido pelo Docker, informe `FINTRACK_WEB_URL` com a URL local do container e `FINTRACK_API_URL` com a API descartável correspondente. O frontend precisa apontar para essa API, e o CORS deve permitir sua origem. A validação visual também verifica que os valores dos cartões do resumo permanecem em uma única linha.

