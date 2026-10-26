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

using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class HostingFlowTests
{
    [Fact]
    public void Production_rejects_development_jwt_key()
    {
        using var app = new FinTrackApiFactory().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Jwt:Key", "dev-only-change-this-key-before-production-123456"));
        Assert.Throws<InvalidOperationException>(() => app.CreateClient());
    }

    [Fact]
    public async Task Production_redirects_to_https_and_only_allows_configured_origin()
    {
        await using var app = new FinTrackApiFactory().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Jwt:Key", "production-test-key-with-more-than-32-bytes")
            .UseSetting("Cors:AllowedOrigins:0", "https://app.fintrack.test"));
        using var http = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var redirect = await http.GetAsync("/health");
        Assert.Equal(HttpStatusCode.TemporaryRedirect, redirect.StatusCode);
        Assert.Equal("https://localhost/health", redirect.Headers.Location?.ToString());

        using var https = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://fintrack.test") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://app.fintrack.test");
        var response = await https.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.Equal("https://app.fintrack.test", response.Headers.GetValues("Access-Control-Allow-Origin").Single());

        using var deniedRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        deniedRequest.Headers.Add("Origin", "https://other.fintrack.test");
        var denied = await https.SendAsync(deniedRequest);
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Health_check_returns_database_status_and_correlation_id()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", "test-correlation-id");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("test-correlation-id", response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Contains("\"database\":\"Healthy\"", await response.Content.ReadAsStringAsync());
    }
}
