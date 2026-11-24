$ErrorActionPreference = 'Stop'
$pruneScript = Join-Path $PSScriptRoot '../database/prune-audit.ps1'
$state = @{ Query = ''; Database = 'FinTrack' }

# Intercepta o Docker: este teste nunca conecta ao banco nem exclui dados.
function docker {
    $global:LASTEXITCODE = 0
    if ($args -contains 'config') {
        return @{ services = @{ api = @{ environment = @{ ConnectionStrings__DefaultConnection = "Server=sqlserver;Database=$($state.Database);" } } } } | ConvertTo-Json -Depth 6
    }
    $state.Query = $args[-1]
}

& $pruneScript
if ($state.Query -notmatch 'DECLARE @Apply bit = 0;' -or $state.Query -notmatch 'IF @Apply = 1' -or $state.Query -notmatch 'DATEADD\(day, -365, SYSUTCDATETIME\(\)\)') {
    throw 'A simulacao deve usar 365 dias sem executar DELETE.'
}
& $pruneScript -RetentionDays 90 -Apply
if ($state.Query -notmatch 'DECLARE @Apply bit = 1;' -or $state.Query -notmatch 'DELETE TOP \(10000\)' -or $state.Query -notmatch 'OccurredAtUtc < @Cutoff' -or $state.Query -notmatch 'DATEADD\(day, -90,') {
    throw 'A limpeza deve limitar a exclusao e preservar o instante de corte.'
}
$rejected = $false
try { & $pruneScript -RetentionDays 0 } catch { $rejected = $true }
if (!$rejected) { throw 'Retencao zero deve ser rejeitada.' }
$state.Database = 'Invalid]Database'
$state.Query = ''
$rejected = $false
try { & $pruneScript -Apply } catch { $rejected = $true }
if (!$rejected -or $state.Query) { throw 'Nome de banco invalido deve impedir a execucao do SQL.' }
Write-Host 'PASS: simulacao, limite de exclusao, corte UTC, retencao e nome do banco.'
