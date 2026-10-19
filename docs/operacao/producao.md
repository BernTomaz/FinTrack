# Preparação para produção

O Compose atual é de desenvolvimento: a API usa `Development` e o frontend usa `ng serve`. Não exponha esse ambiente diretamente na internet. A hospedagem e o certificado TLS precisam ser escolhidos antes da publicação.

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

O backup deve ter frequência, retenção e destino definidos conforme a perda de dados tolerável. Mantenha uma cópia protegida fora da máquina do banco; o teste local não substitui essa operação.

No SQL Server, use `BACKUP DATABASE` com `COPY_ONLY, CHECKSUM` para um backup manual de validação sem alterar a sequência dos backups diferenciais. O diretório deve ser acessível pela conta do serviço SQL Server.

Para testar uma cópia:

1. Executar `RESTORE VERIFYONLY ... WITH CHECKSUM` no arquivo.
2. Conferir os nomes lógicos com `RESTORE FILELISTONLY`.
3. Restaurar com outro nome de banco e `WITH MOVE` para arquivos novos. Não usar `REPLACE` nem sobrescrever o banco original.
4. Comparar usuários, contas, categorias, lançamentos e saldos. Testar a aplicação apontando para a cópia, quando necessário.
5. Remover apenas o banco de verificação e os arquivos temporários identificados após conferir os resultados.

A validação automatizada de desenvolvimento em `scripts/validation/check-database.mjs` ensaia backup, restauração e comparação de contagens e checksums em bancos descartáveis. Os checksums são uma verificação prática de regressão, não uma prova criptográfica de igualdade.
