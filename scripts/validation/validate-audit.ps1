param([switch]$BrowserOnly)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$composeFile = Join-Path $root 'docker-compose.yml'
$suffix = [guid]::NewGuid().ToString('N')
$validationDatabase = "FinTrackAuditValidation_$suffix"
$validationContainer = "fintrack-audit-validation-$suffix"
$apiStarted = $false
$savedEnvironment = @{}
foreach ($key in @('FINTRACK_TEST_SQLSERVER', 'CHROME_BIN', 'FINTRACK_API_URL', 'FINTRACK_WEB_URL', 'FINTRACK_WEB_PORT')) {
    $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}

function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command falhou (codigo $LASTEXITCODE)." }
}
function Wait-Healthy([string]$Url) {
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        try { if ((Invoke-RestMethod -Uri $Url -TimeoutSec 2).status -eq 'Healthy') { return } } catch {}
        Start-Sleep -Seconds 1
    }
    throw "API indisponivel em $Url."
}

Push-Location $root
try {
    if (!$BrowserOnly) { Invoke-Checked docker @('compose', '-f', $composeFile, 'up', '-d', '--build') }
    Wait-Healthy 'http://localhost:5080/health'
    $config = (& docker compose -f $composeFile config --format json | Out-String) | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel resolver o Compose.' }
    $connection = $config.services.api.environment.ConnectionStrings__DefaultConnection
    if ($connection -notmatch '(?i)(?:^|;)Server=sqlserver,1433;') {
        throw 'A validacao exige o SQL Server local do Compose.'
    }
    $env:FINTRACK_TEST_SQLSERVER = $connection -replace '(?i)Server=sqlserver,1433;', 'Server=localhost,1433;'
    $chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
    if (Test-Path -LiteralPath $chrome) { $env:CHROME_BIN = $chrome }
    if (!$BrowserOnly) {
        Invoke-Checked dotnet @('test', 'tests/FinTrack.Tests/FinTrack.Tests.csproj', '-m:1', '--settings', 'tests/FinTrack.Tests/coverlet.runsettings', '--collect:XPlat Code Coverage', '--results-directory', 'out/coverage')
        Invoke-Checked pwsh @('-NoProfile', '-File', 'scripts/validation/check-audit-retention.ps1')

        Push-Location (Join-Path $root 'src/FinTrack.Web')
        try {
            Invoke-Checked npm @('run', 'build')
            Invoke-Checked npm @('test', '--', '--watch=false', '--browsers=ChromiumHeadless')
        } finally { Pop-Location }
    } elseif (!(Test-Path -LiteralPath 'src/FinTrack.Web/dist/FinTrack.Web/browser/index.html')) {
        throw 'Build do frontend ausente. Execute a validacao sem -BrowserOnly.'
    }

    $temporaryConnection = $connection -replace '(?i)(Database=)[a-zA-Z0-9_]+(?=;|$)', "Database=$validationDatabase"
    if ($temporaryConnection -eq $connection) { throw 'Banco descartavel nao configurado.' }
    Invoke-Checked docker @('compose', '-f', $composeFile, 'run', '-d', '--no-deps', '--name', $validationContainer,
        '-p', '5083:5080', '-e', "ConnectionStrings__DefaultConnection=$temporaryConnection",
        '-e', 'Cors__AllowedOrigins__0=http://localhost:4203', 'api')
    $apiStarted = $true
    Wait-Healthy 'http://127.0.0.1:5083/health'
    $env:FINTRACK_API_URL = 'http://127.0.0.1:5083'
    $env:FINTRACK_WEB_URL = 'http://localhost:4203'
    $env:FINTRACK_WEB_PORT = '4203'
    Invoke-Checked node @('scripts/validation/check-ui.mjs')
    Invoke-Checked docker @('compose', '-f', $composeFile, 'ps')
    if ($BrowserOnly) { Write-Host 'PASS: navegador com API e banco descartavel (sem repetir rebuild e testes anteriores).' }
    else { Write-Host 'PASS: rebuild Docker, SQL Server, rollback, retencao, cobertura, frontend e navegador.' }
} finally {
    try {
        if ($apiStarted) {
            & docker rm -f $validationContainer | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Falha ao remover $validationContainer." }
            if ($validationDatabase -notmatch '^FinTrackAuditValidation_[a-f0-9]{32}$') { throw 'Banco descartavel invalido.' }
            $query = "IF DB_ID(N'$validationDatabase') IS NOT NULL BEGIN ALTER DATABASE [$validationDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$validationDatabase]; END;"
            $runSql = 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; sqlcmd=/opt/mssql-tools18/bin/sqlcmd; if [ ! -x "$sqlcmd" ]; then sqlcmd=/opt/mssql-tools/bin/sqlcmd; fi; exec "$sqlcmd" -S localhost -U sa -C -b -Q "$1"'
            Invoke-Checked docker @('compose', '-f', $composeFile, 'exec', '-T', 'sqlserver', 'sh', '-c', $runSql, 'fintrack-audit-validation', $query)
        }
    } finally {
        foreach ($key in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($key, $savedEnvironment[$key], 'Process') }
        Pop-Location
    }
}
