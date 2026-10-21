# Backup local com Docker

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
