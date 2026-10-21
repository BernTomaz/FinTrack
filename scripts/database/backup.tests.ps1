$ErrorActionPreference = 'Stop'
$testDirectory = Join-Path $PSScriptRoot ('../../out/FinTrackBackup_' + [guid]::NewGuid().ToString('N'))
$global:backupTestCalls = @()
$global:backupTestFailure = ''

function global:docker {
    $global:backupTestCalls += ,@($args)
    $global:LASTEXITCODE = 0
    if ($args -contains $global:backupTestFailure) {
        $global:LASTEXITCODE = 1
        return
    }
    if ($args -contains 'config') {
        return '{"services":{"api":{"environment":{"ConnectionStrings__DefaultConnection":"Server=sqlserver;Database=FinTrackDb;User Id=sa"}}}}'
    }
    if ($args -contains 'cp') {
        [System.IO.File]::WriteAllText($args[-1], 'fake backup')
    }
}

try {
    & "$PSScriptRoot/backup.ps1" -OutputDirectory $testDirectory
    if (@(Get-ChildItem $testDirectory -Filter '*.bak').Count -ne 1) { throw 'Backup local nao criado.' }
    $sqlCall = $global:backupTestCalls | Where-Object { $_ -contains '-Q' -or $_ -contains 'sh' } | Select-Object -First 1
    if ($sqlCall[-1] -notlike '*COPY_ONLY, CHECKSUM*RESTORE VERIFYONLY*WITH CHECKSUM*') { throw 'Verificacao SQL ausente.' }
    if ($sqlCall[-2] -ne 'fintrack-backup') { throw 'Argumento zero do shell ausente; a consulta nao chegaria em $1.' }

    foreach ($failure in @('exec', 'cp')) {
        $global:backupTestFailure = $failure
        $global:backupTestCalls = @()
        $failed = $false
        try { & "$PSScriptRoot/backup.ps1" -OutputDirectory $testDirectory } catch { $failed = $true }
        if (!$failed) { throw "Falha de $failure nao propagada." }
        if ($global:backupTestCalls | Where-Object { $_ -contains 'rm' }) { throw 'Backup removido apos falha.' }
    }
    if (@(Get-ChildItem $testDirectory -Filter '*.bak').Count -ne 1) { throw 'Backup anterior alterado.' }
    Write-Host 'OK: backup, verificacao SQL e falhas de execucao/copia.'
} finally {
    Remove-Item Function:/docker
    Remove-Variable backupTestCalls, backupTestFailure -Scope Global
    # O caminho e criado acima, exclusivamente para este teste.
    if (Test-Path -LiteralPath $testDirectory) { Remove-Item -LiteralPath $testDirectory -Recurse -Force }
}
