# Validação da versão atual — 30/10/2026

Revisão validada: `85f42af2ee52f6012748be5e013a0a038a621373`.

- CI do mesmo commit concluído com sucesso: https://github.com/BernTomaz/FinTrack/actions/runs/40872497426.
- Backend: 68 testes aprovados, zero falhas e nenhum ignorado, incluindo SQL Server real.
- Frontend: 30 testes aprovados em cinco arquivos, com Chromium.
- Build Angular de produção aprovado.
- Fluxo completo no navegador aprovado: cadastro, login, contas, categorias, lançamentos, edição, exclusão, filtros, paginação, exportação e novo login.
- Duplicação validada com revisão antes de salvar e preservação do original, incluindo cancelamento em largura mobile.
- Fluxo diário validado com 31 dias, despesas do dia e saldo acumulado sem saldo inicial das contas.
- Clique na categoria do dashboard abriu os relatórios com tipo Expense e identificador da categoria na consulta; foram encontrados os 54 lançamentos de despesa esperados.
- 43 capturas do fluxo completo, em desktop e larguras mobile simuladas de 320 a 1440 pixels, sem transbordamento horizontal nem erros de JavaScript. Uma captura adicional registra o filtro por categoria.

Os testes de navegador usaram uma API separada e o banco descartável `FinTrackValidation_20261030_01a18d5f`, sem dados pessoais. As capturas e o resultado do fluxo ficam em `out/validation/ui`, fora do Git.

As primeiras tentativas no ambiente restrito não acessaram SQL Server e o navegador instalado. A execução com acesso aos recursos locais concluiu com sucesso, sem alteração de código da aplicação. O build do backend apresentou dois avisos xUnit2012 existentes em InfrastructureTests; eles não impediram a validação.

Não foram repetidos auditorias de dependências, benchmark de volume ou ensaio de restauração. Não houve publicação nem teste em celular físico. O próximo passo é escolher hospedagem e preparar homologação conforme `producao.md`.
