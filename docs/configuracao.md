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

Para rodar localmente, a API precisa apontar para o SQL Server da sua máquina.

Confira o nome do servidor no SSMS e configure a `DefaultConnection` local com user-secrets.

Exemplo:

```text
Server=NOME_DO_SERVIDOR;Database=FinTrackDb
```

Na sua máquina pode ser `localhost`, `SQLEXPRESS`, o nome do computador ou outro nome exibido no SSMS.

O importante é: a API e o SSMS precisam usar o mesmo servidor e o mesmo banco.

### 1. Ajustar o servidor do banco local

Configure a `DefaultConnection` local para usar o mesmo servidor que você abre no SSMS:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=NOME_DO_SERVIDOR;Database=FinTrackDb;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True" --project src\FinTrack.Api
```

Troque `NOME_DO_SERVIDOR` pelo nome que aparece no SSMS.

Exemplos:

```text
Server=localhost;Database=FinTrackDb
Server=.\SQLEXPRESS;Database=FinTrackDb
Server=NOME_DO_COMPUTADOR;Database=FinTrackDb
```

Esse comando salva a conexão apenas na sua máquina e não altera o arquivo versionado do projeto.

A API cria ou atualiza o banco ao iniciar.

### 2. Rodar o backend

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

### 3. Rodar o frontend

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

### Dados não aparecem no SSMS

Confira se o servidor aberto no SSMS é o mesmo da `DefaultConnection` configurada localmente.

Se a API estiver usando `Server=NOME_DO_SERVIDOR`, consulte o banco `FinTrackDb` dentro desse mesmo servidor.
