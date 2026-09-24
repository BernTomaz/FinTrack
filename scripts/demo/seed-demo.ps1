param(
    [string]$ApiUrl = "http://localhost:5080",
    [string]$Email = "demo@fintrack.local",
    [string]$Password = "Senha@123"
)

try {
    $auth = Invoke-RestMethod -Method Post -Uri "$ApiUrl/auth/register" -ContentType "application/json" -Body (@{
        name = "Demo FinTrack"
        email = $Email
        password = $Password
    } | ConvertTo-Json)
} catch {
    $auth = Invoke-RestMethod -Method Post -Uri "$ApiUrl/auth/login" -ContentType "application/json" -Body (@{
        email = $Email
        password = $Password
    } | ConvertTo-Json)
}

$headers = @{ Authorization = "Bearer $($auth.token)" }

$account = Invoke-RestMethod -Method Post -Uri "$ApiUrl/accounts" -Headers $headers -ContentType "application/json" -Body (@{
    name = "Conta Demo"
    type = "Checking"
    initialBalance = 500
    openingDate = "2026-01-01"
} | ConvertTo-Json)

$incomeCategory = Invoke-RestMethod -Method Post -Uri "$ApiUrl/categories" -Headers $headers -ContentType "application/json" -Body (@{
    name = "Salario"
    type = "Income"
} | ConvertTo-Json)

$expenseCategory = Invoke-RestMethod -Method Post -Uri "$ApiUrl/categories" -Headers $headers -ContentType "application/json" -Body (@{
    name = "Mercado"
    type = "Expense"
} | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri "$ApiUrl/transactions" -Headers $headers -ContentType "application/json" -Body (@{
    accountId = $account.id
    categoryId = $incomeCategory.id
    type = "Income"
    amount = 5000
    date = "2026-08-05"
    description = "Receita demo"
} | ConvertTo-Json) | Out-Null

Invoke-RestMethod -Method Post -Uri "$ApiUrl/transactions" -Headers $headers -ContentType "application/json" -Body (@{
    accountId = $account.id
    categoryId = $expenseCategory.id
    type = "Expense"
    amount = 750.50
    date = "2026-08-12"
    description = "Despesa demo"
} | ConvertTo-Json) | Out-Null

[pscustomobject]@{
    Email = $Email
    Password = $Password
    Month = "2026-08"
} | Format-List
