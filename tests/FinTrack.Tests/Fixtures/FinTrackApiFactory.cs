using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.DTOs.Categories;
using FinTrack.Application.DTOs.Dashboard;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace FinTrack.Tests;

internal sealed class FinTrackApiFactory(string? sqlConnection = null) : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        if (sqlConnection is not null)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", sqlConnection);
            return;
        }
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FinTrackDbContext>>();
            for (var index = services.Count - 1; index >= 0; index--)
            {
                var descriptor = services[index];
                if (IsSqlServerProviderService(descriptor))
                {
                    services.RemoveAt(index);
                }
            }

            var databaseProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<FinTrackDbContext>(options =>
                options
                    .UseInMemoryDatabase(_databaseName)
                    .UseInternalServiceProvider(databaseProvider));
        });
    }

    private static bool IsSqlServerProviderService(ServiceDescriptor descriptor) =>
        descriptor.ServiceType.Name == "IDatabaseProvider" ||
        ContainsSqlServer(descriptor.ServiceType) ||
        ContainsSqlServer(descriptor.ImplementationType);

    private static bool ContainsSqlServer(Type? type) =>
        type?.FullName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
}
