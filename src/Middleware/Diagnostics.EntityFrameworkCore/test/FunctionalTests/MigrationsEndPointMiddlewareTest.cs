// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.FunctionalTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests;

public class MigrationsEndPointMiddlewareTest
{
    [Fact]
    public async Task Non_migration_requests_pass_thru()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app
                .UseMigrationsEndPoint()
                .UseMiddleware<SuccessMiddleware>());
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

        Assert.Equal("Request Handled", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    class SuccessMiddleware
    {
        public SuccessMiddleware(RequestDelegate next)
        { }

        public virtual async Task Invoke(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync("Request Handled");
        }
    }

    [Theory]
    [InlineData("GET", null)]
    [InlineData("GET", "application/x-www-form-urlencoded")]
    [InlineData("PUT", "application/x-www-form-urlencoded")]
    [InlineData("DELETE", "application/x-www-form-urlencoded")]
    [InlineData("OPTIONS", null)]
    [InlineData("POST", null)]
    [InlineData("POST", "application/json")]
    [InlineData("POST", "text/plain")]
    [InlineData("POST", "image/png")]
    public async Task Invalid_migration_requests_pass_thru(string method, string contentType)
    {
        foreach (var path in new[] { MigrationsEndPointOptions.DefaultPath, new PathString("/CustomMigrations") })
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseTestServer()
                        .Configure(app => app
                            .UseMigrationsEndPoint(new MigrationsEndPointOptions { Path = path })
                            .UseMiddleware<SuccessMiddleware>());
                }).Build();

            await host.StartAsync();

            using var request = new HttpRequestMessage(new HttpMethod(method), "http://localhost" + path)
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
            };
            if (contentType != null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            using var response = await host.GetTestClient().SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Request Handled", await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public async Task Migration_request_default_path()
    {
        await Migration_request(useCustomPath: false);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public async Task Migration_request_custom_path()
    {
        await Migration_request(useCustomPath: true);
    }

    private async Task Migration_request(bool useCustomPath)
    {
        using (var database = SqlTestStore.CreateScratch())
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(database.ConnectionString);

            var path = useCustomPath ? new PathString("/EndPoints/ApplyMyMigrations") : MigrationsEndPointOptions.DefaultPath;

            using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    if (useCustomPath)
                    {
                        app.UseMigrationsEndPoint(new MigrationsEndPointOptions
                        {
                            Path = path
                        });
                    }
                    else
                    {
                        app.UseMigrationsEndPoint();
                    }
                })
                .ConfigureServices(services =>
                {
                    services.AddDbContext<BloggingContextWithMigrations>(options =>
                    {
                        options.UseSqlite(database.ConnectionString);
                    });
                });
            }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();

            using (var db = BloggingContextWithMigrations.CreateWithoutExternalServiceProvider(optionsBuilder.Options))
            {
                var databaseCreator = db.GetService<IRelationalDatabaseCreator>();
                Assert.False(databaseCreator.Exists());

                var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("context", typeof(BloggingContextWithMigrations).AssemblyQualifiedName)
                    });

                HttpResponseMessage response = await server.CreateClient()
                    .PostAsync("http://localhost" + path, formData);

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

                Assert.True(databaseCreator.Exists());

                var historyRepository = db.GetService<IHistoryRepository>();
                var appliedMigrations = historyRepository.GetAppliedMigrations();
                Assert.Equal(2, appliedMigrations.Count);
                Assert.Equal("111111111111111_MigrationOne", appliedMigrations.ElementAt(0).MigrationId);
                Assert.Equal("222222222222222_MigrationTwo", appliedMigrations.ElementAt(1).MigrationId);
            }
        }
    }

    [Theory]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData("application/x-www-form-urlencoded; charset=utf-8")]
    [InlineData("multipart/form-data; boundary=test-boundary")]
    public async Task Context_type_not_specified(string contentType)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseMigrationsEndPoint();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        using HttpContent formData = contentType.StartsWith("multipart/", StringComparison.Ordinal)
            ? new MultipartFormDataContent("test-boundary") { { new StringContent("value"), "other" } }
            : new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());
        formData.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(StringsHelpers.GetResourceString("MigrationsEndPointMiddleware_NoContextType"), content);
        Assert.True(content.Length > 512);
    }

    [Fact]
    public async Task Invalid_context_type_specified()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseMigrationsEndPoint();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var typeName = "You won't find this type ;)";
        var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("context", typeName)
                });

        var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(StringsHelpers.GetResourceString("FormatMigrationsEndPointMiddleware_ContextNotRegistered", typeName), content);
        Assert.True(content.Length > 512);
    }

    [Fact]
    public async Task Context_not_registered_in_services()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app.UseMigrationsEndPoint())
                .ConfigureServices(services => services.AddEntityFrameworkSqlite());
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("context", typeof(BloggingContext).AssemblyQualifiedName)
                });

        var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(StringsHelpers.GetResourceString("FormatMigrationsEndPointMiddleware_ContextNotRegistered", typeof(BloggingContext).AssemblyQualifiedName), content);
        Assert.True(content.Length > 512);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public async Task Exception_while_applying_migrations()
    {
        using (var database = SqlTestStore.CreateScratch())
        {
            using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app.UseMigrationsEndPoint())
                .ConfigureServices(services =>
                {
                    services.AddDbContext<BloggingContextWithSnapshotThatThrows>(optionsBuilder =>
                    {
                        optionsBuilder.UseSqlite(database.ConnectionString);
                    });
                });
            }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();

            var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("context", typeof(BloggingContextWithSnapshotThatThrows).AssemblyQualifiedName)
                    });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData));

            Assert.StartsWith(StringsHelpers.GetResourceString("FormatMigrationsEndPointMiddleware_Exception", typeof(BloggingContextWithSnapshotThatThrows)), ex.Message);
            Assert.Equal("Welcome to the invalid snapshot!", ex.InnerException.Message);
        }
    }
}
