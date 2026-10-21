param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../../out/backups')
)

$ErrorActionPreference = 'Stop'
$composeFile = Join-Path $PSScriptRoot '../../docker-compose.yml'

function Invoke-Docker {
    & docker compose -f $composeFile @args
    if ($LASTEXITCODE -ne 0) {
        throw "Docker falhou (codigo $LASTEXITCODE). Confira se o Docker e o SQL Server estao em execucao."
    }
}

# Ler a configuracao resolvida respeita o .env sem exibir credenciais.
$config = (Invoke-Docker config --format json | Out-String) | ConvertFrom-Json
$connection = $config.services.api.environment.ConnectionStrings__DefaultConnection
if ($connection -notmatch '(?:^|;)\s*Database=([a-zA-Z0-9_]{1,128})\s*(?:;|$)') {
    throw 'Nome do banco ausente ou nao suportado na configuracao do Compose.'
}
$database = $Matches[1]
$filename = '{0}_{1}_{2}.bak' -f $database, (Get-Date -Format 'yyyyMMdd_HHmmss'), ([guid]::NewGuid().ToString('N'))
$containerPath = "/var/opt/mssql/data/$filename"
$directory = New-Item -ItemType Directory -Force -Path $OutputDirectory
$destination = Join-Path $directory.FullName $filename
$partial = "$destination.partial"

$query = "BACKUP DATABASE [$database] TO DISK = N'$containerPath' WITH COPY_ONLY, CHECKSUM; RESTORE VERIFYONLY FROM DISK = N'$containerPath' WITH CHECKSUM;"
$runSql = 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; sqlcmd=/opt/mssql-tools18/bin/sqlcmd; if [ ! -x "$sqlcmd" ]; then sqlcmd=/opt/mssql-tools/bin/sqlcmd; fi; exec "$sqlcmd" -S localhost -U sa -C -b -Q "$1"'
Invoke-Docker exec -T sqlserver sh -c $runSql fintrack-backup $query

try {
    Invoke-Docker cp "sqlserver:$containerPath" $partial
    if ((Get-Item -LiteralPath $partial).Length -eq 0) {
        throw 'O arquivo copiado esta vazio.'
    }
    Move-Item -LiteralPath $partial -Destination $destination
} catch {
    throw "Falha ao salvar o backup local. A copia no container foi preservada em ${containerPath}. $($_.Exception.Message)"
}

Write-Host "Backup salvo e verificado: $destination"
try {
    Invoke-Docker exec -T sqlserver rm -- $containerPath
} catch {
    Write-Warning "Backup local salvo, mas a copia temporaria permanece no container: $containerPath"
}
