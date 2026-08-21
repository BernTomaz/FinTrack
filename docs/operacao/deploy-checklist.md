# Checklist de Entrega

O deploy público fica fora do MVP, mas o projeto já deve estar pronto para uma entrega local validada.

## Fechamento do MVP

- [x] Confirmar que `docker compose up --build` sobe SQL Server, API e frontend.
- [x] Validar o health check da API em `/health`.
- [x] Criar um usuário de teste pelo frontend.
- [x] Criar uma conta, uma categoria de receita e uma categoria de despesa.
- [x] Criar uma receita e uma despesa.
- [x] Conferir o dashboard mensal após os lançamentos.
- [x] Validar que o gráfico de fluxo de caixa exibe apenas meses com lançamentos reais.
- [x] Exportar o CSV de lançamentos.
- [x] Validar que contas com lançamentos não podem ser excluídas diretamente.
- [x] Validar a exclusão de lançamentos pela interface.
- [x] Validar que a conta pode ser excluída após remover seus lançamentos.
- [x] Revisar as telas principais no desktop e no mobile.
- [x] Rodar os testes automatizados do backend.
- [x] Rodar o build de produção do frontend.

## Antes de Publicar

- Configurar variáveis de ambiente.
- Usar uma chave JWT forte.
- Conferir a connection string do banco.
- Rodar as migrations.
- Conferir CORS para a URL real do frontend.
- Configurar HTTPS.
- Revisar logs de inicialização da API.
- Validar backup e restauração do banco, se houver ambiente persistente.

