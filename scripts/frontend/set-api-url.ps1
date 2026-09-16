param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl
)

$indexPath = Join-Path $PSScriptRoot "..\..\src\FinTrack.Web\src\index.html"
$content = Get-Content $indexPath -Raw
$updated = [regex]::Replace(
    $content,
    '<meta name="fintrack-api-url" content="[^"]*">',
    "<meta name=`"fintrack-api-url`" content=`"$ApiUrl`">")

Set-Content -Path $indexPath -Value $updated -NoNewline
