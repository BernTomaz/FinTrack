# Validação de segurança e preparação para publicação — 27/10/2026

## Correções

- CLI e build Angular atualizados de 21.2.27 para 21.2.28, mantendo o framework Angular 21.
- Dependências transitivas atualizadas nas faixas compatíveis do lockfile.
- Override de Undici 7.30.1 restrito ao build Angular, até a correção ser incorporada pelo pacote.
- Cópia interna de Undici do npm 11.21.1 atualizada para 6.29.1 na imagem Docker. O ajuste temporário e sua condição de remoção estão documentados no Dockerfile e em `docs/configuracao.md`.

## Verificações locais

- `npm audit`: zero vulnerabilidades, incluindo dependências de desenvolvimento.
- Trivy na imagem completa `fintrack-validation-web:latest`: zero vulnerabilidades altas ou críticas detectadas. A análise foi local, usando a base de vulnerabilidades disponível em 27/10/2026; isso não garante ausência de falhas desconhecidas.
- Build Angular de produção aprovado.
- 23 testes Angular aprovados.
- SQL Server, API e frontend Docker saudáveis em um projeto Compose separado, com portas e volume temporários.
- Fluxo completo no navegador aprovado: cadastro, login, contas, categorias, receitas, despesas, edição, exclusão, filtros, paginação, dashboard, CSV e saída/novo login.
- 42 capturas de desktop e larguras mobile simuladas, sem erros de JavaScript ou transbordamento horizontal.
- Testes existentes do script de backup aprovados, incluindo cenários de falha de execução e cópia.

## Ensaio real de restauração

O banco fictício `FinTrackDockerValidation`, criado no ambiente Docker temporário, foi copiado com `BACKUP DATABASE ... COPY_ONLY, CHECKSUM`. O arquivo passou por `RESTORE VERIFYONLY WITH CHECKSUM` e foi restaurado em `FinTrackDockerValidation_Restore`, com arquivos novos e sem `REPLACE`.

`DBCC CHECKDB` não apontou erros. Contagem e checksum dos dados restaurados corresponderam à origem nas tabelas:

| Tabela | Linhas |
| --- | ---: |
| Users | 1 |
| Accounts | 1 |
| Categories | 2 |
| Transactions | 55 |

A origem não foi sobrescrita. A cópia de restauração foi removida ao terminar. Os containers, volume e backup do ambiente descartável também são removidos após a validação. Os relatórios detalhados ficam em `out/validation/`, fora do Git.

## Publicação e limites

A hospedagem ainda não foi escolhida. O roteiro em [Preparação para produção](producao.md) descreve artefatos, homologação, segredos, HTTPS, banco persistente, primeiro administrador, backup e reversão. Nenhum serviço foi contratado ou publicado.

Foi escolhida a validação mobile simulada. Não foi executado teste em celular físico. O ensaio local de restauração não valida as permissões, discos, retenção ou recuperação de uma futura hospedagem; esse ensaio deve ser repetido no ambiente escolhido antes da publicação.

O CI deve ser conferido na execução correspondente aos commits enviados, sem reutilizar o resultado de uma revisão anterior.
