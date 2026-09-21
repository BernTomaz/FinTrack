# Deploy

## Status

Deploy público ainda está pendente.

## Objetivo

Publicar o FinTrack com backend, frontend e banco configurados de forma segura para demonstração.

## Checklist

- Escolher hospedagem.
- Configurar banco SQL Server.
- Definir `ConnectionStrings__DefaultConnection`.
- Configurar chave JWT segura.
- Configurar URL pública da API no frontend.
- Rodar migrations.
- Validar health check.
- Validar cadastro, login, conta, categoria, lançamento, dashboard e CSV.

## Cuidados

- Não versionar `.env` real.
- Não usar senha padrão em produção.
- Não expor segredos no frontend.
- Validar CORS de acordo com o domínio publicado.
