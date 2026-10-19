# Validação de melhorias — 19/10/2026

## Escopo

Validação local em Windows, Chrome e SQL Server 2022, com banco descartável separado do FinTrackDb. Nenhum dado pessoal foi usado. O backend foi executado em Debug e o frontend com build de produção.

## Interface

Os fluxos exercitados incluem cadastro seguido de login, criação de conta e categorias, receitas e despesas, edição de lançamento, filtros, bloqueio de alterações incompatíveis, bloqueio de exclusão de conta com lançamentos e download de CSV.

Foram verificadas as larguras 320, 390, 768, 1024 e 1440 pixels. A revisão encontrou e corrigiu:

- Botões do cabeçalho cortados no dashboard estreito: as ações agora quebram de linha.
- Efeito de fundo do cartão de login causando transbordamento em 320 pixels: sua área agora respeita o cartão.
- Menu compacto cobrindo o conteúdo no celular: corrigida a prioridade do espaçamento lateral.
- Gráfico vazio com uma única categoria de despesa: o círculo completo agora usa dois arcos externos e dois internos.
- Idioma do documento definido como inglês: alterado para `pt-BR`.

O CSS do gráfico antigo, sem uso no projeto, foi removido. O aviso de tamanho do CSS principal permanece; não houve remoção de regras usadas apenas para reduzir o número do build.

## Desempenho

Um usuário fictício com 120 mil lançamentos, distribuídos em 12 meses, sendo 10 mil no mês consultado. Sete medições após uma requisição de aquecimento, em localhost, com o mesmo usuário e os índices originais. Os valores são medianas observadas, não garantias para produção.

| Endpoint | Antes | Depois | Resposta aproximada |
| --- | ---: | ---: | ---: |
| Dashboard mensal | 1.316 ms | 227 ms | 953 bytes |
| Lançamentos do mês | 143 ms | 115 ms | 2,8 MB |
| CSV mensal | 98 ms | 93 ms | 478 kB |

O ganho relevante foi no dashboard: redução observada de aproximadamente 83%. A consulta que juntava cada lançamento à categoria produzia um plano ruim para usuários com históricos muito diferentes. A solução soma por categoria no SQL Server e busca os nomes separadamente, transferindo apenas totais e cadastros, sem carregar os lançamentos para agrupá-los na aplicação. O dashboard fica com cinco consultas simples, em vez das sete originais.

Um índice ampliado foi ensaiado e descartado: não trouxe ganho frente ao índice original. A melhoria final não exige migration.

A resposta de 2,8 MB para 10 mil lançamentos mostra um limite de crescimento: paginação deve ser avaliada se esse volume mensal se tornar comum. Não foi implementada nesta rodada para manter o contrato atual da API e o MVP simples. As diferenças pequenas em listagem e CSV podem ser variação da máquina.

## Segurança e operação

- A configuração JWT é validada na inicialização. Chave curta, emissor/audiência ausentes e expiração inválida são recusados; a chave de desenvolvimento também é recusada fora de Development.
- CORS aceita origens configuradas; as origens locais padrão ficam restritas ao desenvolvimento.
- Em produção, há redirecionamento para HTTPS, HSTS e tratamento do protocolo encaminhado por proxies locais confiáveis.
- Dois cadastros simultâneos foram testados em banco vazio: ambos funcionaram e apenas um recebeu o papel de administrador.
- Backup com checksum foi verificado e restaurado em outro banco temporário. Contagens e checksums de usuários, contas, categorias e lançamentos conferiram com a origem.
- O procedimento de configuração, atualização e restauração está em [Preparação para produção](producao.md).

## Limites

Não houve publicação, teste com certificado/domínio real, uso de aparelho físico ou simulação de rede móvel. As medições são sequenciais; não representam concorrência de muitos usuários. Backup automático, retenção e cópia externa dependem do ambiente de hospedagem e não foram configurados nesta máquina.

## Fechamento

- 53 testes backend aprovados, incluindo SQL Server real e as proteções de produção.
- 12 testes frontend aprovados, incluindo o gráfico de categoria única.
- Build de produção aprovado, com o aviso já existente de tamanho do CSS.
- 41 capturas de interface verificadas, incluindo tema escuro e menu compacto; nenhum erro de execução no navegador.
- API de validação encerrada, banco descartável removido e exclusão dos backups temporários conferida pelo SQL Server. As capturas e medições ficam em `out/validation`, fora do versionamento.
