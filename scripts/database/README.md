# Operação do banco com Docker

## Retenção da auditoria

A política operacional inicial mantém 365 dias de eventos. A limpeza é manual; não há exclusão na inicialização da API, endpoint de limpeza ou agendamento automático. O responsável pelo ambiente deve executar a rotina periodicamente. Enquanto ela não for executada, eventos mais antigos continuam armazenados e consultáveis.

Na raiz do repositório, simule primeiro:

```powershell
.\scripts\database\prune-audit.ps1
```

O script usa o mesmo banco do Compose da API, mostra o instante de corte UTC e a quantidade elegível, sem mostrar snapshots ou credenciais. Depois de conferir o destino, o corte e seu backup, aplique explicitamente:

```powershell
.\scripts\database\prune-audit.ps1 -Apply
```

O SQL está em `prune-audit.sql`, compartilhado com o teste de integração em banco descartável. O comando PowerShell fornece o corte UTC e o modo de execução; mantenha os dois arquivos juntos.

São excluídos até 10.000 eventos anteriores ao corte por execução. Eventos exatamente no corte são preservados. Se `EligibleRemaining` for maior que zero, repita a operação até concluir. Contas, categorias, lançamentos e usuários não são removidos. O comando é irreversível sem restauração do backup.

`-RetentionDays` permite ajustar o prazo (1 a 36.500 dias); use o mesmo valor na simulação e na aplicação. Mudanças no prazo devem ser registradas pelo responsável. O padrão de 365 dias é uma decisão inicial de produto, não uma declaração de conformidade. Esta rotina atende ao Docker local; a hospedagem precisa de uma rotina equivalente com destino, permissões e periodicidade definidos.

Backups têm retenção independente: apagar eventos no banco não apaga cópias antigas. Após restaurar um backup, reaplique a política antes de liberar o ambiente. Um evento removido deixa de aparecer no histórico; a auditoria não oferece desfazer ou recuperação automática.

Teste das proteções do script, sem conectar ao Docker ou ao banco:

```powershell
.\scripts\validation\check-audit-retention.ps1
```

Para reconstruir o Docker e executar a validação completa (SQL Server, rollback, retenção em banco descartável, cobertura, frontend e navegador), use um terminal com acesso ao Docker Desktop:

```powershell
.\scripts\validation\validate-audit.ps1
```

O script preserva o banco de uso e deixa a stack principal atualizada e ativa. Os testes SQL e o navegador usam bancos descartáveis. São necessários .NET 10, Node, dependências do frontend instaladas, PowerShell e Chrome instalado ou Chromium do Playwright. As portas 5083 e 4203 devem estar disponíveis. Relatórios de cobertura ficam em `out/coverage` e capturas de navegador em `out/validation/ui`.

Se somente a navegação ficou pendente, execute `.\scripts\validation\validate-audit.ps1 -BrowserOnly` na raiz. Essa opção reutiliza as imagens e o build existentes, sem repetir rebuild e suítes já aprovadas. O navegador respeita `PLAYWRIGHT_BROWSER_PATH` e, em seguida, `CHROME_BIN`; sem ambos, usa o Chromium gerenciado pelo Playwright.

## Execução do backup

Requisitos: PowerShell, Docker Compose e o servico `sqlserver` do projeto em execucao, com `.env` configurado. O script usa o banco definido na conexao da API no Compose e as credenciais do container, sem exibir a senha. Destina-se ao ambiente Docker local, nao ao SQL Server instalado diretamente no Windows.

Da raiz do repositorio:

```powershell
.\scripts\database\backup.ps1
```

O arquivo fica em `out/backups`, com nome do banco, data/hora local e identificador unico. Para outro destino:

```powershell
.\scripts\database\backup.ps1 -OutputDirectory 'D:\Backups\FinTrack'
```

O script executa `BACKUP DATABASE` com `COPY_ONLY, CHECKSUM` e `RESTORE VERIFYONLY WITH CHECKSUM` antes da copia. O arquivo local recebe a extensao `.bak` apenas depois da copia concluida. Em caso de falha na copia, o backup do container e preservado e seu caminho e informado; um arquivo `.partial` local pode permanecer. Depois do sucesso, apenas a copia temporaria do container e removida. Nenhum banco ou backup anterior e excluido.

Os arquivos nao entram no Git. Eles contem dados pessoais: restrinja o acesso e mantenha uma copia protegida fora da maquina. Nao ha agendamento nem exclusao automatica por idade; execute conforme sua rotina e acompanhe o espaco disponivel. A verificacao de integridade nao substitui uma restauracao real.

## Testar a restauracao sem sobrescrever o original

1. Escolha um `.bak` e copie para o container:

   ```powershell
   docker compose cp 'D:\Backups\FinTrack\SEU_ARQUIVO.bak' sqlserver:/var/opt/mssql/data/fintrack-restore-check.bak
   ```

2. Abra o cliente SQL no container (a senha e lida do ambiente interno):

   ```powershell
   docker compose exec sqlserver sh -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b'
   ```

   Se a imagem usar a versao antiga das ferramentas, troque o caminho por `/opt/mssql-tools/bin/sqlcmd`.

3. Consulte os nomes logicos dos arquivos:

   ```sql
   RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/data/fintrack-restore-check.bak';
   GO
   ```

4. Confirme que `FinTrackRestoreCheck` nao existe. Substitua `NOME_LOGICO_DADOS` e `NOME_LOGICO_LOG` pelos nomes retornados acima e execute:

   ```sql
   RESTORE DATABASE [FinTrackRestoreCheck]
   FROM DISK = N'/var/opt/mssql/data/fintrack-restore-check.bak'
   WITH MOVE N'NOME_LOGICO_DADOS' TO N'/var/opt/mssql/data/FinTrackRestoreCheck.mdf',
        MOVE N'NOME_LOGICO_LOG' TO N'/var/opt/mssql/data/FinTrackRestoreCheck_log.ldf',
        CHECKSUM;
   GO
   DBCC CHECKDB ([FinTrackRestoreCheck]) WITH NO_INFOMSGS;
   GO
   ```

   Use nomes novos para banco e arquivos em cada ensaio. Nunca acrescente `REPLACE`. Se o backup listar mais arquivos, inclua um `MOVE` para cada um.

5. Confira contas, categorias, lancamentos e saldos da copia com os valores esperados no momento do backup. Se validar pela aplicacao, use uma API separada apontada para a copia. Encerre o cliente com `QUIT`. A remocao do banco de verificacao e do arquivo copiado e manual, depois de conferir os nomes; o script de backup nao faz essa limpeza.

## Teste do script

```powershell
.\scripts\database\backup.tests.ps1
```

O teste simula o Docker e cobre criacao do arquivo, SQL de verificacao e falhas de execucao/copia, preservando backups anteriores. Nao valida SQL Server nem substitui o ensaio de restauracao.
