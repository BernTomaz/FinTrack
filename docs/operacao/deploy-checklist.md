# Checklist de Entrega

O deploy público fica fora do MVP, mas o projeto já deve estar pronto para uma entrega local validada.

## Fechamento do MVP

- Confirmar que `docker compose up --build` sobe SQL Server, API e frontend.
- Validar o health check da API em `/health`.
- Criar um usuário de teste pelo frontend.
- Criar uma conta, uma categoria e um lançamento.
- Conferir o dashboard mensal após o lançamento.
- Exportar o CSV de lançamentos.
- Revisar as telas principais no desktop e no mobile.
- Rodar os testes automatizados do backend.
- Rodar o build de produção do frontend.

## Antes de Publicar

- Configurar variáveis de ambiente.
- Usar uma chave JWT forte.
- Conferir a connection string do banco.
- Rodar as migrations.
- Conferir CORS para a URL real do frontend.
- Configurar HTTPS.
- Revisar logs de inicialização da API.
- Validar backup e restauração do banco, se houver ambiente persistente.

