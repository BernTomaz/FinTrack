# Validação de melhorias — 20/10/2026

## SQL Server no CI

O job Backend usa Ubuntu e um serviço SQL Server 2022 temporário. A variável `FINTRACK_TEST_SQLSERVER` está configurada para que o teste de integração deixe de ser ignorado. O teste cria seu próprio banco e o exclui ao terminar. As credenciais fixas do workflow pertencem somente ao contêiner descartável do CI.

Validação local: 54 testes backend aprovados, nenhum ignorado, incluindo SQL Server real. A primeira execução hospedada ainda depende do envio ao GitHub.

## Paginação

`GET /transactions` retorna `{ items, totalCount, page, pageSize }`. O padrão é 50 itens e o máximo é 100. Filtros, busca e ordenação são aplicados no servidor antes da paginação, com desempate por criação e identificador. Os relatórios usam páginas de 50 itens com Anterior/Próxima; o dashboard busca apenas os cinco lançamentos recentes. Seu gráfico usa os totais mensais, sem depender dos itens da página. CSV permanece completo.

Os testes cobrem 55 lançamentos em duas páginas, páginas vazias, limites inválidos, filtros, busca por descrição/conta/categoria/data, ordenação, isolamento entre usuários e CSV completo. Os 14 testes Angular passaram, incluindo cancelamento de consultas antigas e de buscas pendentes ao sair.

Clientes externos que esperavam um array precisam ler `items` e percorrer as páginas. Não há migration nem alteração nos dados existentes. A paginação por posição reflete os dados atuais: alterações simultâneas entre duas consultas podem mudar a composição das páginas.

Não foi repetida a medição com 120 mil lançamentos nem realizado teste com muitos usuários simultâneos. O script de volume foi ajustado ao contrato paginado e passou pela checagem de sintaxe; medições anteriores da listagem completa não são diretamente comparáveis.

## Navegador

Playwright foi fixado como dependência de desenvolvimento, permitindo reproduzir o script existente no CI com Chromium. O job inicia uma API com banco descartável, verifica a saúde da API, executa o fluxo e publica capturas/logs como artefato `browser-validation`. A API é encerrada ao terminar e o contêiner é descartado com o job.

Validação local em Windows, SQL Server e Chrome sem interface, com frontend em build de produção: cadastro, login, contas, categorias, receitas, despesas, edição, bloqueios de alterações/exclusão, filtros, dashboard, exportação e novo login. A extensão de paginação verifica 50 itens na primeira página, cinco na segunda, busca além da primeira página e exportação dos 55 lançamentos. O total do gráfico confere mesmo com apenas cinco itens no dashboard.

Resultado: 42 capturas com verificações automáticas, larguras de 320, 390, 768, 1024 e 1440 pixels, temas claro/escuro e menu compacto; nenhum erro de execução no navegador. Os novos controles foram inspecionados visualmente em desktop e mobile. Não houve teste em aparelho físico ou rede móvel. Evidências locais em `out/validation/ui`.

A API temporária foi encerrada e a exclusão do banco descartável foi confirmada no SQL Server. O banco de uso pessoal não foi utilizado.

## CSS e documentação

Removidas regras sem uso de `.form-line`. Os controles de página quebram de linha em telas estreitas e identificam navegação, carregamento e botões desabilitados. A fonte principal ganhou uma verificação no teste visual.

Build de produção aprovado: aproximadamente 349,64 kB iniciais, estimativa de transferência de 86,88 kB. O CSS principal permanece em aproximadamente 21,84 kB, acima do orçamento de aviso de 18 kB e abaixo do limite de erro de 24 kB. Não houve aumento de orçamento; uma redução maior do CSS continua pendente.

Atualizados status, roadmap, endpoints, wiki e instruções de testes. Durante a instalação, `npm audit` reportou 26 alertas na árvore de dependências (10 moderados, 13 altos e três críticos). O diff do lockfile adiciona apenas Playwright e Playwright Core; os alertas abrangem dependências existentes, incluindo Angular e ferramentas de build/teste. Atualizar e validar essas dependências fica para uma rodada própria.

## Pendências

- Confirmar os jobs no GitHub após o envio.
- Reduzir mais o CSS e tratar os alertas de dependências.
- Medir volume, concorrência e comportamento em dispositivo/rede reais quando houver ambiente representativo.
- Escolher hospedagem e configurar produção, backup automático, retenção e cópia externa. Essas etapas não foram executadas nesta rodada.
