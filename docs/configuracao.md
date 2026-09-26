# Como rodar o FinTrack

Este guia mostra como rodar o projeto localmente ou com Docker.

## Pré-requisitos

Para rodar localmente:

- .NET 10 SDK
- Node.js 20.19 ou superior
- Angular CLI
- SQL Server LocalDB ou SQL Server local

Para rodar com Docker:

- Docker Desktop

Execute os comandos na raiz do projeto, onde ficam `FinTrack.slnx` e `docker-compose.yml`.

## Rodar localmente, sem Docker

Use este processo quando quiser rodar o backend e o frontend diretamente na máquina, pelo Visual Studio, VS Code ou terminal.

Não use este processo junto com o Docker ao mesmo tempo, porque a API usa a mesma porta nos dois modos.

Por padrão, o backend usa SQL Server LocalDB, uma versão leve do SQL Server para desenvolvimento local:

```text
Server=(localdb)\MSSQLLocalDB;Database=FinTrackDb
```

Se o SQL Server LocalDB estiver instalado e funcionando, não precisa configurar o banco manualmente. A API cria ou atualiza o banco ao iniciar.

Se você usa SQL Server instalado como serviço, como `localhost`, `SQLEXPRESS` ou outra instância aberta pelo SSMS, veja a seção [Usar SQL Server local em vez de LocalDB](#usar-sql-server-local-em-vez-de-localdb).

### Backend

Pelo Visual Studio:

1. Abra a solução.
2. Selecione `FinTrack.Api`.
3. Clique em executar.

Pelo terminal:

```powershell
dotnet restore FinTrack.slnx -m:1
dotnet build FinTrack.slnx --no-restore -m:1
dotnet run --project src\FinTrack.Api
```

API:

- `http://localhost:5080`
- `http://localhost:5080/swagger`
- `http://localhost:5080/health`

### Frontend

Abra outro terminal.

Na primeira vez que clonar o repositório, instale as dependências:

```powershell
cd src\FinTrack.Web
npm install
```

Depois disso, para rodar o frontend no dia a dia, use apenas:

```powershell
cd src\FinTrack.Web
npm start
```

Frontend:

- `http://localhost:4200`

## Rodar com Docker

Use este processo quando quiser rodar tudo via Docker: banco, API e frontend.

Não use este processo junto com a API local ao mesmo tempo, porque a API usa a mesma porta nos dois modos: `5080`.

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Endereços:

- Frontend: `http://localhost:4201`
- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Health check: `http://localhost:5080/health`

Para parar:

```powershell
docker compose down
```

Para parar e apagar os dados do banco Docker:

```powershell
docker compose down -v
```

## Usar SQL Server local em vez de LocalDB

Altere a `DefaultConnection` em `src\FinTrack.Api\appsettings.json`:

```json
"DefaultConnection": "Server=localhost;Database=FinTrackDb;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"
```

Se o servidor tiver outro nome, troque `localhost`.

## Dados de demonstração

Com a API rodando:

```powershell
.\scripts\demo\seed-demo.ps1
```

## Testes

Backend:

```powershell
dotnet test tests\FinTrack.Tests\FinTrack.Tests.csproj --no-restore -m:1
```

Frontend:

```powershell
cd src\FinTrack.Web
npm run build
```

## Problemas comuns

### Porta 5080 em uso

Pare a API no Visual Studio ou encerre os containers:

```powershell
docker compose down
```

Para descobrir qual processo usa a porta:

```powershell
netstat -ano | findstr :5080
```

### LocalDB não conecta

Teste:

```powershell
sqllocaldb info MSSQLLocalDB
```

Se falhar, repare o SQL Server LocalDB, use SQL Server local ou rode com Docker.
