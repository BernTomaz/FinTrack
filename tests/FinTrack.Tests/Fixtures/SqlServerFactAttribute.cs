namespace FinTrack.Tests;

internal sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FINTRACK_TEST_SQLSERVER")))
        {
            Skip = "Defina FINTRACK_TEST_SQLSERVER para testar em um banco SQL Server temporário.";
        }
    }
}
