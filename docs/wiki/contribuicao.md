# Contribuição

## Princípio

Manter o FinTrack simples e focado no MVP.

## Antes de Alterar

- Entenda o fluxo financeiro impactado.
- Reuse padrões existentes.
- Evite dependências novas sem ganho claro.
- Evite abstrações especulativas.
- Preserve responsividade mobile.

## Validações Recomendadas

Antes de rodar localmente, confira [Configuração local](../configuracao.md) e ajuste a `DefaultConnection` para o SQL Server da sua máquina.

Backend:

```powershell
dotnet test tests\FinTrack.Tests\FinTrack.Tests.csproj --no-restore -m:1
```

Frontend:

```powershell
cd src\FinTrack.Web
npm run build
```

Docker:

```powershell
docker compose up -d --build
```

## Documentação

Ao alterar comportamento visível, atualize a documentação relacionada em `docs/` e, se necessário, as páginas de `docs/wiki/`.
