# Fechamento das pendências — 20/10/2026

## Dependências

Angular atualizado de 20 para 21.2.25, CLI/build para 21.2.27. Karma foi substituído por Vitest 4.1.12 com Playwright/Chromium: as versões antigas do executor e suas dependências não tinham correções compatíveis. Os mesmos 14 testes foram mantidos e passaram no navegador. A instalação limpa e a auditoria npm terminaram com zero vulnerabilidades conhecidas. O Docker do frontend agora usa `npm ci` para reproduzir o lockfile.

Pacotes Microsoft .NET atualizados para 10.0.13; a dependência transitiva System.Security.Cryptography.Pkcs foi explicitamente fixada em 9.0.21, versão corrigida segundo o [aviso da Microsoft](https://github.com/advisories/GHSA-846f-f764-76mx). A auditoria NuGet de todos os projetos, incluindo transitivas, não reportou vulnerabilidades conhecidas. Os 54 testes backend passaram, sem ignorados, incluindo SQL Server real.

## CSS

Regras de layout responsivo separadas em `app-responsive.css`, mantendo a ordem da cascata. Removidas declarações de espaçamento e largura que eram sempre sobrescritas por regras globais de maior prioridade. A redução real do código CSS foi de aproximadamente 220 bytes; a divisão dos arquivos não representa, por si só, redução equivalente do CSS transferido. O build passou sem avisos de orçamento, mantendo os limites existentes por arquivo.

O bundle inicial ficou em 354,82 kB, com estimativa de transferência de 87,88 kB. A atualização do framework aumentou ligeiramente o JavaScript em relação ao build anterior; não se atribui ganho de desempenho à reorganização do CSS.

Fluxo completo no build de produção aprovado novamente: 42 capturas e nenhum erro de execução, com larguras de 320, 390, 768, 1024 e 1440 pixels, temas claro/escuro e menu compacto. O ensaio usa navegador em computador; não substitui um aparelho físico.

## Volume e concorrência

API local em Debug, SQL Server 2022, banco descartável com 120 mil lançamentos e 10 mil no mês consultado. Sete amostras após uma de aquecimento. A primeira rodada coincidiu com instalações/compilação e não foi usada na tabela abaixo; repetiram-se apenas as medições com os mesmos dados.

| Endpoint | Mediana sequencial | Resposta |
| --- | ---: | ---: |
| Dashboard | 419 ms | 953 bytes |
| Lançamentos, página de 50 | 89 ms | 14.103 bytes |
| CSV completo do mês | 105 ms | 477.530 bytes |

A listagem transferiu aproximadamente 99,5% menos dados que a resposta completa de 2,8 MB registrada em 19/10, porque agora retorna 50 itens. Isso compara tamanhos de respostas com quantidades diferentes, não prova aceleração equivalente da consulta.

Oito consultas simultâneas, cinco requisições por executor, 40 requisições por endpoint; todos os resultados conferidos, sem erros:

| Endpoint | Mediana | Percentil 95 | Requisições por segundo |
| --- | ---: | ---: | ---: |
| Dashboard | 712 ms | 1.934 ms | 6 |
| Página de lançamentos | 147 ms | 395 ms | 30 |

São resultados locais com um usuário fictício, sem meta de latência definida e sem representar infraestrutura de produção ou muitos usuários distintos. O dashboard continua sendo o endpoint mais custoso sob concorrência. Não foram adicionados índices ou cache sem evidência adicional.

O backup com checksum também foi restaurado em outro banco temporário e as quatro tabelas conferiram em contagem/checksum. Os resultados ficam em `out/validation/performance-result.json` e `database-result.json`, fora do versionamento.

## CI

A [execução 39437244264](https://github.com/BernTomaz/FinTrack/actions/runs/39437244264) passou nos testes SQL/backend e nos testes/build frontend, mas falhou no script do navegador: após clicar em Contas, o dashboard ainda continha dois campos Nome. O script agora espera o título e o componente de destino antes de preencher formulários. A instalação do Chromium e o comando do executor frontend foram atualizados no workflow.

A confirmação da nova execução hospedada será registrada após o envio dos commits. A validação em aparelho físico permanece pendente de acesso a um dispositivo real.
