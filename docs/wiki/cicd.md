# CI/CD

## Status

CI implementado com GitHub Actions.

## Workflow

O workflow principal fica em:

```text
.github/workflows/ci.yml
```

## Validações

- Restore do backend.
- Build do backend.
- Testes automatizados do backend, incluindo SQL Server 2022 temporário no job Ubuntu.
- Instalação de dependências do frontend.
- Build de produção do frontend.
- Testes Angular com Vitest e Chromium sem interface no job Windows.
- Fluxo Playwright contra API e banco descartáveis: cadastro, login, lançamentos, paginação, dashboard e CSV.
- Verificações de largura de 320 a 1440 pixels, tema escuro e menu compacto.
- Capturas e logs publicados como artefato `browser-validation`, inclusive quando há falha.

O teste SQL cria e exclui seu próprio banco. O banco do navegador e o contêiner SQL são descartados com o job. Na primeira execução hospedada, backend e frontend passaram, mas o fluxo de navegador encontrou uma corrida ao trocar de tela. O script agora espera o título e o componente de destino antes de preencher formulários. A nova execução será registrada no relatório de fechamento.

## Deploy

Deploy público ainda não foi definido. O CI valida o estado do projeto antes dessa etapa.
