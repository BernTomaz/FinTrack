# Preparação para produção

O Compose atual é de desenvolvimento: a API usa `Development` e o frontend usa `ng serve`. Não exponha esse ambiente diretamente na internet. A hospedagem e o certificado TLS precisam ser escolhidos antes da publicação.

## Roteiro enquanto a hospedagem não foi escolhida

1. Escolher onde servir os arquivos estáticos do frontend, executar a API .NET 10 e manter o SQL Server com armazenamento persistente. Anotar as URLs HTTPS e os endereços do proxy antes de configurar CORS e cabeçalhos encaminhados.
2. Criar um ambiente de homologação com banco separado e dados fictícios. Injetar as configurações abaixo pelo mecanismo de segredos da hospedagem; não versionar credenciais nem reutilizar a chave de desenvolvimento.
3. Gerar os artefatos na raiz do repositório:

   ```powershell
   dotnet publish src/FinTrack.Api/FinTrack.Api.csproj -c Release -o out/publish/api
   # Substituir pela URL HTTPS real da API de homologação.
   ./scripts/frontend/set-api-url.ps1 -ApiUrl 'https://api.exemplo.invalid'
   cd src/FinTrack.Web
   npm ci
   npm run build
   ```

   O comando de URL altera `src/index.html`; gerar o artefato por ambiente e revisar essa alteração antes de qualquer commit. A URL de exemplo não é um serviço publicado. O frontend entregue é `src/FinTrack.Web/dist/FinTrack.Web/browser`; não usar `ng serve` como servidor público.
4. Configurar HTTPS, persistência, segredos e origens exatas. Publicar apenas uma instância da API para aplicar migrations na inicialização. Cadastrar o administrador com acesso restrito antes de abrir o cadastro ao público.
5. Conferir `/health`, cadastro/login, contas, categorias, receitas, despesas, edição, exclusão, filtros, saldos e CSV na homologação. Verificar também que as origens não autorizadas são recusadas.
6. Fazer e restaurar um backup em outro banco no serviço escolhido. Registrar frequência, retenção, destino protegido, responsável e prazo de recuperação. O ensaio local não confirma permissões ou armazenamento da hospedagem.
7. Liberar a publicação somente depois dessas verificações e definir como voltar à versão anterior. Uma reversão de binários não garante compatibilidade com migrations novas.

Permanecem por escolher: hospedagem, domínio, certificados, proxies confiáveis e política de backup de produção. Nenhum serviço precisa ser contratado para executar os testes locais.

## Configuração mínima

- Definir `ASPNETCORE_ENVIRONMENT=Production`.
- Fornecer `ConnectionStrings__DefaultConnection` por segredo do ambiente, com um usuário do banco dedicado ao FinTrack.
- Fornecer `Jwt__Key` com pelo menos 32 bytes e gerada aleatoriamente. A API recusa a chave de desenvolvimento fora de `Development`. Manter a mesma chave entre reinícios; trocá-la invalida sessões existentes.
- Definir `Jwt__Issuer`, `Jwt__Audience` e `Jwt__ExpirationMinutes` conforme o ambiente.
- Configurar `Cors__AllowedOrigins__0` com a origem HTTPS exata do frontend; adicionar índices seguintes apenas quando necessário. Em produção não há liberação automática das origens locais.
- Gerar o frontend com `npm ci` e `npm run build`, configurar `fintrack-api-url` para a API HTTPS e servir os arquivos de `dist/FinTrack.Web/browser` em um servidor de arquivos estáticos.
- Encerrar HTTPS no servidor/proxy com certificado válido. A API redireciona HTTP para HTTPS na porta 443 e envia HSTS em requisições HTTPS.

Se o proxy estiver na mesma máquina, conectar a API apenas ao endereço local e encaminhar `X-Forwarded-Proto` com o protocolo real da conexão. A API aceita esse cabeçalho somente de proxies locais por padrão. Um proxy remoto ou em outro contêiner exige configurar explicitamente os endereços confiáveis antes de publicar; não liberar cabeçalhos encaminhados de qualquer origem.

Cadastre o administrador enquanto a aplicação ainda estiver restrita ao responsável. O primeiro cadastro continua recebendo esse papel. Os cadastros no SQL Server são serializados em uma transação para impedir que duas requisições simultâneas gerem dois administradores.

## Atualização

1. Conferir os testes, o build e a migration da versão.
2. Fazer backup e confirmar que existe uma restauração recente testada.
3. Parar a API e aplicar a atualização em uma janela de manutenção. A API atual aplica migrations na inicialização; usar apenas uma instância durante essa etapa.
4. Esta rodada de melhorias não exige migration ou alteração nos dados existentes.
5. Validar `/health`, login, saldos, edição e exportação antes de liberar o acesso.
6. Se falhar, manter a aplicação indisponível e avaliar a compatibilidade do banco antes de voltar a versão. Não restaurar um backup sobre dados mais recentes sem avaliar a perda de alterações.

## Backup e restauração

Para o ambiente Docker local, use o [script de backup](../../scripts/database/README.md), que verifica o backup e copia o arquivo para a máquina. Ele não configura uma rotina de produção nem substitui o ensaio de restauração descrito abaixo.

O backup deve ter frequência, retenção e destino definidos conforme a perda de dados tolerável. Mantenha uma cópia protegida fora da máquina do banco; o teste local não substitui essa operação.

No SQL Server, use `BACKUP DATABASE` com `COPY_ONLY, CHECKSUM` para um backup manual de validação sem alterar a sequência dos backups diferenciais. O diretório deve ser acessível pela conta do serviço SQL Server.

Para testar uma cópia:

1. Executar `RESTORE VERIFYONLY ... WITH CHECKSUM` no arquivo.
2. Conferir os nomes lógicos com `RESTORE FILELISTONLY`.
3. Restaurar com outro nome de banco e `WITH MOVE` para arquivos novos. Não usar `REPLACE` nem sobrescrever o banco original.
4. Comparar usuários, contas, categorias, lançamentos e saldos. Testar a aplicação apontando para a cópia, quando necessário.
5. Remover apenas o banco de verificação e os arquivos temporários identificados após conferir os resultados.

A validação automatizada de desenvolvimento em `scripts/validation/check-database.mjs` ensaia backup, restauração e comparação de contagens e checksums em bancos descartáveis. Os checksums são uma verificação prática de regressão, não uma prova criptográfica de igualdade.
