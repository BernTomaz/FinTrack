dotnet ef migrations script `
  --project src\FinTrack.Infrastructure `
  --startup-project src\FinTrack.Api `
  --no-build `
  --idempotent `
  --output docker\sqlserver\fintrack-migrations.sql
