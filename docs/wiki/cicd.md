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
- Testes automatizados do backend.
- Instalação de dependências do frontend.
- Build de produção do frontend.

## Deploy

Deploy público ainda não foi definido. O CI valida o estado do projeto antes dessa etapa.
