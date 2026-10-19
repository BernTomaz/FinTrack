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

## Estratégia

Os testes cobrem regras de negócio e fluxos principais da API, incluindo autenticação, isolamento por usuário, validações, dashboard, exportação CSV e regras de exclusão.

As regressões também verificam alterações de contas e categorias com lançamentos existentes, campos obrigatórios nulos, o último dia do ano 9999 e saldos históricos em meses vazios e anos bissextos.

## Execução

```powershell
dotnet test FinTrack.slnx -m:1
cd src/FinTrack.Web
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

Os testes Angular verificam carregamento inicial, reaproveitamento de contas e categorias, atualização após edição e exclusão, falhas de carregamento, troca rápida de mês e limpeza dos dados ao sair. O CI executa os testes do backend e do frontend.

## Verificação com SQL Server

O teste abaixo executa o dashboard no SQL Server real, incluindo migrations, agrupamentos, saldos, limites de mês e isolamento por usuário. Sem a variável de conexão, apenas esse teste é marcado como ignorado; os demais usam o banco em memória.

Execute a partir da raiz, usando uma instância de desenvolvimento com permissão para criar e excluir bancos:

```powershell
$env:FINTRACK_TEST_SQLSERVER = 'Server=localhost;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True'
dotnet test FinTrack.slnx -m:1 --filter Dashboard_queries_work_on_sql_server
Remove-Item Env:FINTRACK_TEST_SQLSERVER
```

O teste substitui o nome do banco da conexão por `FinTrackTests_<identificador aleatório>` e exclui somente esse banco ao terminar. Não use credenciais de produção. Se o processo for interrompido à força, o banco temporário pode precisar ser removido manualmente.

## Validação visual e de volume

Os scripts em `scripts/validation` usam o padrão de Playwright já empregado na captura dos GIFs, sem adicionar uma dependência ao frontend. O pacote pode ser fornecido por `PLAYWRIGHT_PATH` e o Chrome por `PLAYWRIGHT_BROWSER_PATH`.

Use uma API local separada, ligada a um banco novo `FinTrackValidation_<GUID>`, e configure `FINTRACK_API_URL` para ela. Nunca aponte esses scripts para os dados de uso pessoal.

- `node scripts/validation/check-ui.mjs`: serve o build de produção em `localhost:4201`; valida cadastro, login, contas, categorias, lançamentos, filtros, bloqueios de edição/exclusão, exportação, tema e larguras de 320 a 1440 pixels. Salva capturas e resultados em `out/validation/ui`.
- `node scripts/validation/check-database.mjs`: exige `FINTRACK_VALIDATION_DATABASE`, autenticação integrada no SQL Server local e `sqlcmd`. Confere que a API usa o banco informado, insere 120 mil lançamentos fictícios e mede sete requisições após uma de aquecimento. Também restaura um backup em outro banco temporário e compara os dados.
- `FINTRACK_BENCHMARK_REUSE=true` reutiliza o usuário de volume existente e repete apenas a medição, sem inserir lançamentos ou fazer outro backup.

O teste de restauração exclui sua própria cópia do banco. Ao terminar a sessão, encerrar a API temporária e remover o banco de origem descartável e os arquivos de backup gerados, conferindo os nomes antes da exclusão. Esses scripts não fazem teste de concorrência de muitos usuários nem simulam uma rede móvel real.

