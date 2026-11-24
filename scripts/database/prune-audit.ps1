param(
    [ValidateRange(1, 36500)]
    [int]$RetentionDays = 365,
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$composeFile = Join-Path $PSScriptRoot '../../docker-compose.yml'

function Invoke-Docker {
    & docker compose -f $composeFile @args
    if ($LASTEXITCODE -ne 0) { throw "Docker falhou (codigo $LASTEXITCODE)." }
}

# Resolve o mesmo banco da API sem imprimir a conexao ou credenciais.
$config = (Invoke-Docker config --format json | Out-String) | ConvertFrom-Json
$connection = $config.services.api.environment.ConnectionStrings__DefaultConnection
if ($connection -notmatch '(?:^|;)\s*Database=([a-zA-Z0-9_]{1,128})\s*(?:;|$)') {
    throw 'Nome do banco ausente ou nao suportado na configuracao do Compose.'
}
$database = $Matches[1]
$sql = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'prune-audit.sql') -Raw).Replace('$(DatabaseName)', $database)
$query = "DECLARE @Cutoff datetime2 = DATEADD(day, -$RetentionDays, SYSUTCDATETIME()); DECLARE @Apply bit = $([int][bool]$Apply); " + $sql
if ($Apply) {
    # Limita o impacto e preserva todos os eventos no instante de corte ou posteriores.
    Write-Host "Aplicando retencao de $RetentionDays dias no banco $database (ate 10000 eventos)."
} else {
    Write-Host "Simulacao no banco $database. Nenhum evento sera excluido. Use -Apply para aplicar."
}
$runSql = 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; sqlcmd=/opt/mssql-tools18/bin/sqlcmd; if [ ! -x "$sqlcmd" ]; then sqlcmd=/opt/mssql-tools/bin/sqlcmd; fi; exec "$sqlcmd" -S localhost -U sa -C -b -Q "$1"'
Invoke-Docker exec -T sqlserver sh -c $runSql fintrack-audit-prune $query
