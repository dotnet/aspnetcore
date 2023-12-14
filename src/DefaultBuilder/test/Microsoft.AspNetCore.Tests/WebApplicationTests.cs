// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: HostingStartup(typeof(WebApplicationTests.TestHostingStartup))]
[assembly: UserSecretsId("UserSecret-TestId")]

namespace Microsoft.AspNetCore.Tests;

public class WebApplicationTests
{
    public delegate WebApplicationBuilder CreateBuilderFunc();
    public delegate WebApplicationBuilder CreateBuilderArgsFunc(string[] args);
    public delegate WebApplicationBuilder CreateBuilderOptionsFunc(WebApplicationOptions options);
    public delegate WebApplicationBuilder WebApplicationBuilderConstructorFunc(WebApplicationOptions options, Action<IHostBuilder> configureDefaults);

    private static WebApplicationBuilder CreateBuilder() => WebApplication.CreateBuilder();
    private static WebApplicationBuilder CreateSlimBuilder() => WebApplication.CreateSlimBuilder();
    private static WebApplicationBuilder CreateEmptyBuilder()
    {
        var builder = WebApplication.CreateEmptyBuilder(new());
        // CreateEmptyBuilder doesn't register an IServer or Routing.
        builder.Services.AddRoutingCore();
        builder.WebHost.UseKestrelCore();
        return builder;
    }

    public static IEnumerable<object[]> CreateBuilderFuncs
    {
        get
        {
            yield return new[] { (CreateBuilderFunc)CreateBuilder };
            yield return new[] { (CreateBuilderFunc)CreateSlimBuilder };
            yield return new[] { (CreateBuilderFunc)CreateEmptyBuilder };
        }
    }

    public static IEnumerable<object[]> CreateNonEmptyBuilderFuncs
    {
        get
        {
            yield return new[] { (CreateBuilderFunc)CreateBuilder };
            yield return new[] { (CreateBuilderFunc)CreateSlimBuilder };
        }
    }

    private static WebApplicationBuilder CreateBuilderArgs(string[] args) => WebApplication.CreateBuilder(args);
    private static WebApplicationBuilder CreateSlimBuilderArgs(string[] args) => WebApplication.CreateSlimBuilder(args);
    private static WebApplicationBuilder CreateEmptyBuilderArgs(string[] args)
    {
        var builder = WebApplication.CreateEmptyBuilder(new() { Args = args });
        // CreateEmptyBuilder doesn't register an IServer or Routing.
        builder.Services.AddRoutingCore();
        builder.WebHost.UseKestrelCore();
        return builder;
    }

    public static IEnumerable<object[]> CreateBuilderArgsFuncs
    {
        get
        {
            yield return new[] { (CreateBuilderArgsFunc)CreateBuilderArgs };
            yield return new[] { (CreateBuilderArgsFunc)CreateSlimBuilderArgs };
            yield return new[] { (CreateBuilderArgsFunc)CreateEmptyBuilderArgs };
        }
    }

    public static IEnumerable<object[]> CreateNonEmptyBuilderArgsFuncs
    {
        get
        {
            yield return new[] { (CreateBuilderArgsFunc)CreateBuilderArgs };
            yield return new[] { (CreateBuilderArgsFunc)CreateSlimBuilderArgs };
        }
    }

    private static WebApplicationBuilder CreateBuilderOptions(WebApplicationOptions options) => WebApplication.CreateBuilder(options);
    private static WebApplicationBuilder CreateSlimBuilderOptions(WebApplicationOptions options) => WebApplication.CreateSlimBuilder(options);
    private static WebApplicationBuilder CreateEmptyBuilderOptions(WebApplicationOptions options)
    {
        var builder = WebApplication.CreateEmptyBuilder(options);
        // CreateEmptyBuilder doesn't register an IServer or Routing.
        builder.Services.AddRoutingCore();
        builder.WebHost.UseKestrelCore();
        return builder;
    }

    public static IEnumerable<object[]> CreateBuilderOptionsFuncs
    {
        get
        {
            yield return new[] { (CreateBuilderOptionsFunc)CreateBuilderOptions };
            yield return new[] { (CreateBuilderOptionsFunc)CreateSlimBuilderOptions };
            yield return new[] { (CreateBuilderOptionsFunc)CreateEmptyBuilderOptions };
        }
    }

    public static IEnumerable<object[]> CreateNonEmptyBuilderOptionsFuncs
    {
        get
        {
            yield return new[] { (CreateBuilderOptionsFunc)CreateBuilderOptions };
            yield return new[] { (CreateBuilderOptionsFunc)CreateSlimBuilderOptions };
        }
    }

    private static WebApplicationBuilder WebApplicationBuilderConstructor(WebApplicationOptions options, Action<IHostBuilder> configureDefaults)
        => new WebApplicationBuilder(options, configureDefaults);
    private static WebApplicationBuilder WebApplicationSlimBuilderConstructor(WebApplicationOptions options, Action<IHostBuilder> configureDefaults)
        => new WebApplicationBuilder(options, slim: true, configureDefaults);
    private static WebApplicationBuilder WebApplicationEmptyBuilderConstructor(WebApplicationOptions options, Action<IHostBuilder> configureDefaults)
    {
        var builder = new WebApplicationBuilder(options, slim: false, empty: true, configureDefaults);
        // CreateEmptyBuilder doesn't register an IServer.
        builder.WebHost.UseKestrelCore();
        return builder;
    }

    public static IEnumerable<object[]> WebApplicationBuilderConstructorFuncs
    {
        get
        {
            yield return new[] { (WebApplicationBuilderConstructorFunc)WebApplicationBuilderConstructor };
            yield return new[] { (WebApplicationBuilderConstructorFunc)WebApplicationSlimBuilderConstructor };
            yield return new[] { (WebApplicationBuilderConstructorFunc)WebApplicationEmptyBuilderConstructor };
        }
    }

    [Theory]
    [MemberData(nameof(CreateBuilderArgsFuncs))]
    public async Task WebApplicationBuilder_New(CreateBuilderArgsFunc createBuilder)
    {
        var builder = createBuilder(new string[] { "--urls", "http://localhost:5001" });

        await using var app = builder.Build();
        var newApp = (app as IApplicationBuilder).New();
        Assert.NotNull(newApp.ServerFeatures);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderArgsFuncs))]
    public async Task WebApplicationBuilderConfiguration_IncludesCommandLineArguments(CreateBuilderArgsFunc createBuilder)
    {
        var builder = createBuilder(new string[] { "--urls", "http://localhost:5001" });
        Assert.Equal("http://localhost:5001", builder.Configuration["urls"]);

        var urls = new List<string>();
        var server = new MockAddressesServer(urls);
        builder.Services.AddSingleton<IServer>(server);
        await using var app = builder.Build();

        await app.StartAsync();

        var address = Assert.Single(urls);
        Assert.Equal("http://localhost:5001", address);

        Assert.Same(app.Urls, urls);

        var url = Assert.Single(urls);
        Assert.Equal("http://localhost:5001", url);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationRunAsync_UsesDefaultUrls(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var urls = new List<string>();
        var server = new MockAddressesServer(urls);
        builder.Services.AddSingleton<IServer>(server);
        await using var app = builder.Build();

        await app.StartAsync();

        Assert.Same(app.Urls, urls);

        Assert.Equal(2, urls.Count);
        Assert.Equal("http://localhost:5000", urls[0]);
        Assert.Equal("https://localhost:5001", urls[1]);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationRunUrls_UpdatesIServerAddressesFeature(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var urls = new List<string>();
        var server = new MockAddressesServer(urls);
        builder.Services.AddSingleton<IServer>(server);
        await using var app = builder.Build();

        var runTask = app.RunAsync("http://localhost:5001");

        var url = Assert.Single(urls);
        Assert.Equal("http://localhost:5001", url);

        await app.StopAsync();
        await runTask;
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationUrls_UpdatesIServerAddressesFeature(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var urls = new List<string>();
        var server = new MockAddressesServer(urls);
        builder.Services.AddSingleton<IServer>(server);
        await using var app = builder.Build();

        app.Urls.Add("http://localhost:5002");
        app.Urls.Add("https://localhost:5003");

        await app.StartAsync();

        Assert.Equal(2, urls.Count);
        Assert.Equal("http://localhost:5002", urls[0]);
        Assert.Equal("https://localhost:5003", urls[1]);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationRunUrls_OverridesIServerAddressesFeature(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var urls = new List<string>();
        var server = new MockAddressesServer(urls);
        builder.Services.AddSingleton<IServer>(server);
        await using var app = builder.Build();

        app.Urls.Add("http://localhost:5002");
        app.Urls.Add("https://localhost:5003");

        var runTask = app.RunAsync("http://localhost:5001");

        var url = Assert.Single(urls);
        Assert.Equal("http://localhost:5001", url);

        await app.StopAsync();
        await runTask;
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationUrls_ThrowsInvalidOperationExceptionIfThereIsNoIServerAddressesFeature(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.Services.AddSingleton<IServer>(new MockAddressesServer());
        await using var app = builder.Build();

        Assert.Throws<InvalidOperationException>(() => app.Urls);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task HostedServicesRunBeforeTheServerStarts(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var startOrder = new List<object>();
        var server = new MockServer(startOrder);
        var hostedService = new HostedService(startOrder);
        builder.Services.AddSingleton<IHostedService>(hostedService);
        builder.Services.AddSingleton<IServer>(server);
        await using var app = builder.Build();

        await app.StartAsync();

        Assert.Equal(2, startOrder.Count);
        Assert.Same(hostedService, startOrder[0]);
        Assert.Same(server, startOrder[1]);
    }

    class HostedService : IHostedService
    {
        private readonly List<object> _startOrder;

        public HostedService(List<object> startOrder)
        {
            _startOrder = startOrder;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _startOrder.Add(this);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    class MockServer : IServer
    {
        private readonly List<object> _startOrder;

        public MockServer(List<object> startOrder)
        {
            _startOrder = startOrder;
        }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public void Dispose() { }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
        {
            _startOrder.Add(this);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationRunUrls_ThrowsInvalidOperationExceptionIfThereIsNoIServerAddressesFeature(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.Services.AddSingleton<IServer>(new MockAddressesServer());
        await using var app = builder.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync("http://localhost:5001"));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationRunUrls_ThrowsInvalidOperationExceptionIfServerAddressesFeatureIsReadOnly(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.Services.AddSingleton<IServer>(new MockAddressesServer(new List<string>().AsReadOnly()));
        await using var app = builder.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync("http://localhost:5001"));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilderHost_ThrowsWhenBuiltDirectly(CreateBuilderFunc createBuilder)
    {
        Assert.Throws<NotSupportedException>(() => ((IHostBuilder)createBuilder().Host).Build());
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilderWebHost_ThrowsWhenBuiltDirectly(CreateBuilderFunc createBuilder)
    {
        Assert.Throws<NotSupportedException>(() => ((IWebHostBuilder)createBuilder().WebHost).Build());
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilderWebHostSettingsThatAffectTheHostCannotBeModified(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var webRoot = Path.Combine(contentRoot, "wwwroot");
        var envName = $"{nameof(WebApplicationTests)}_ENV";

        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.ApplicationKey, nameof(WebApplicationTests)));
        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.EnvironmentKey, envName));
        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.ContentRootKey, contentRoot));
        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.WebRootKey, webRoot));
        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "hosting"));
        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, "hostingexclude"));
        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseEnvironment(envName));
        Assert.Throws<NotSupportedException>(() => builder.WebHost.UseContentRoot(contentRoot));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilderWebHostSettingsThatAffectTheHostCannotBeModifiedViaConfigureAppConfiguration(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var webRoot = Path.Combine(contentRoot, "wwwroot");
        var envName = $"{nameof(WebApplicationTests)}_ENV";

        Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                    { WebHostDefaults.ApplicationKey, nameof(WebApplicationTests) }
            });
        }));

        Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                    { WebHostDefaults.EnvironmentKey, envName }
            });
        }));

        Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                    { WebHostDefaults.ContentRootKey, contentRoot }
            });
        }));

        Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                    { WebHostDefaults.WebRootKey, webRoot }
            });
        }));

        Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                    { WebHostDefaults.HostingStartupAssembliesKey, "hosting" }
            });
        }));

        Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                    { WebHostDefaults.HostingStartupExcludeAssembliesKey, "hostingexclude" }
            });
        }));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public void SettingContentRootToSameCanonicalValueWorks(CreateBuilderOptionsFunc createBuilder)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(contentRoot);

        var builder = createBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRoot
        });

        builder.Host.UseContentRoot(contentRoot + Path.DirectorySeparatorChar);
        builder.Host.UseContentRoot(contentRoot.ToUpperInvariant());
        builder.Host.UseContentRoot(contentRoot.ToLowerInvariant());

        builder.WebHost.UseContentRoot(contentRoot + Path.DirectorySeparatorChar);
        builder.WebHost.UseContentRoot(contentRoot.ToUpperInvariant());
        builder.WebHost.UseContentRoot(contentRoot.ToLowerInvariant());
    }

    public static IEnumerable<object[]> CanHandleVariousWebRootPathsData
    {
        get
        {
            foreach (string webRoot in new[] { "wwwroot2", "./wwwroot2", "./bar/../wwwroot2", "foo/../wwwroot2", "wwwroot2/." })
            {
                yield return new object[] { webRoot, (CreateBuilderOptionsFunc)CreateBuilderOptions };
                yield return new object[] { webRoot, (CreateBuilderOptionsFunc)CreateSlimBuilderOptions };
                yield return new object[] { webRoot, (CreateBuilderOptionsFunc)CreateEmptyBuilderOptions };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CanHandleVariousWebRootPathsData))]
    public void WebApplicationBuilder_CanHandleVariousWebRootPaths(string webRoot, CreateBuilderOptionsFunc createBuilder)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRoot);
        var fullWebRootPath = Path.Combine(contentRoot, "wwwroot2");

        try
        {
            var options = new WebApplicationOptions
            {
                ContentRootPath = contentRoot,
                WebRootPath = "wwwroot2"
            };

            var builder = createBuilder(options);

            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
            Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);

            builder.WebHost.UseWebRoot(webRoot);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public void WebApplicationBuilder_CanOverrideWithFullWebRootPaths(CreateBuilderOptionsFunc createBuilder)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRoot);
        var fullWebRootPath = Path.Combine(contentRoot, "wwwroot");
        Directory.CreateDirectory(fullWebRootPath);

        try
        {
            var options = new WebApplicationOptions
            {
                ContentRootPath = contentRoot,
            };

            var builder = createBuilder(options);

            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
            Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);

            builder.WebHost.UseWebRoot(fullWebRootPath);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    public static IEnumerable<object[]> CanHandleVariousWebRootPaths_OverrideDefaultPathData
    {
        get
        {
            foreach (string webRoot in new[] { "wwwroot", "./wwwroot", "./bar/../wwwroot", "foo/../wwwroot", "wwwroot/." })
            {
                yield return new object[] { webRoot, (CreateBuilderOptionsFunc)CreateBuilderOptions };
                yield return new object[] { webRoot, (CreateBuilderOptionsFunc)CreateSlimBuilderOptions };
                yield return new object[] { webRoot, (CreateBuilderOptionsFunc)CreateEmptyBuilderOptions };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CanHandleVariousWebRootPaths_OverrideDefaultPathData))]
    public void WebApplicationBuilder_CanHandleVariousWebRootPaths_OverrideDefaultPath(string webRoot, CreateBuilderOptionsFunc createBuilder)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRoot);
        var fullWebRootPath = Path.Combine(contentRoot, "wwwroot");
        Directory.CreateDirectory(fullWebRootPath);

        try
        {
            var options = new WebApplicationOptions
            {
                ContentRootPath = contentRoot
            };

            var builder = createBuilder(options);

            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
            Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);

            builder.WebHost.UseWebRoot(webRoot);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    public static IEnumerable<object[]> SettingContentRootToRelativePathData
    {
        get
        {
            // Empty behaves differently to null
            foreach (string path in new[] { "", "." })
            {
                yield return new object[] { path, (CreateBuilderOptionsFunc)CreateBuilderOptions };
                yield return new object[] { path, (CreateBuilderOptionsFunc)CreateSlimBuilderOptions };
                yield return new object[] { path, (CreateBuilderOptionsFunc)CreateEmptyBuilderOptions };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SettingContentRootToRelativePathData))]
    public void SettingContentRootToRelativePathUsesAppContextBaseDirectoryAsPathBase(string path, CreateBuilderOptionsFunc createBuilder)
    {
        var builder = createBuilder(new WebApplicationOptions
        {
            ContentRootPath = path
        });

        builder.Host.UseContentRoot(AppContext.BaseDirectory);
        builder.Host.UseContentRoot(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
        builder.Host.UseContentRoot("");

        builder.WebHost.UseContentRoot(AppContext.BaseDirectory);
        builder.WebHost.UseContentRoot(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
        builder.WebHost.UseContentRoot("");

        Assert.Equal(NormalizePath(AppContext.BaseDirectory), NormalizePath(builder.Environment.ContentRootPath));
    }

    private static string NormalizePath(string unnormalizedPath) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(unnormalizedPath));

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ContentRootIsDefaultedToCurrentDirectory()
    {
        var tmpDir = Directory.CreateTempSubdirectory();

        try
        {
            var options = new RemoteInvokeOptions();
            options.StartInfo.WorkingDirectory = tmpDir.FullName;

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                foreach (object[] data in CreateBuilderFuncs)
                {
                    var createBuilder = (CreateBuilderFunc)data[0];
                    var builder = createBuilder();

                    Assert.Equal(NormalizePath(Environment.CurrentDirectory), NormalizePath(builder.Environment.ContentRootPath));
                }
            }, options);
        }
        finally
        {
            tmpDir.Delete(recursive: true);
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    [RemoteExecutionSupported]
    public void ContentRootIsBaseDirectoryWhenCurrentIsSpecialFolderSystem()
    {
        var options = new RemoteInvokeOptions();
        options.StartInfo.WorkingDirectory = Environment.SystemDirectory;

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            foreach (object[] data in CreateBuilderFuncs)
            {
                var createBuilder = (CreateBuilderFunc)data[0];
                var builder = createBuilder();

                Assert.Equal(NormalizePath(AppContext.BaseDirectory), NormalizePath(builder.Environment.ContentRootPath));
            }
        }, options);
    }

    public static IEnumerable<object[]> EnablesAppSettingsConfigurationData
    {
        get
        {
            // Note: CreateEmptyBuilder doesn't enable appsettings.json configuration by default
            yield return new object[] { (CreateBuilderOptionsFunc)CreateBuilderOptions, true };
            yield return new object[] { (CreateBuilderOptionsFunc)CreateBuilderOptions, false };
            yield return new object[] { (CreateBuilderOptionsFunc)CreateSlimBuilderOptions, true };
            yield return new object[] { (CreateBuilderOptionsFunc)CreateSlimBuilderOptions, false };
        }
    }

    [Theory]
    [MemberData(nameof(EnablesAppSettingsConfigurationData))]
    public void WebApplicationBuilderEnablesAppSettingsConfiguration(CreateBuilderOptionsFunc createBuilder, bool isDevelopment)
    {
        var options = new WebApplicationOptions
        {
            EnvironmentName = isDevelopment ? Environments.Development : Environments.Production
        };

        var webApplication = createBuilder(options).Build();

        var config = Assert.IsType<ConfigurationManager>(webApplication.Configuration);
        Assert.Contains(config.Sources, source => source is JsonConfigurationSource jsonSource && jsonSource.Path == "appsettings.json");

        if (isDevelopment)
        {
            Assert.Contains(config.Sources, source => source is JsonConfigurationSource jsonSource && jsonSource.Path == "appsettings.Development.json");
        }
        else
        {
            Assert.DoesNotContain(config.Sources, source => source is JsonConfigurationSource jsonSource && jsonSource.Path == "appsettings.Development.json");
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EmptyWebApplicationBuilderDoesNotEnableAppSettingsConfiguration(bool isDevelopment)
    {
        var options = new WebApplicationOptions
        {
            EnvironmentName = isDevelopment ? Environments.Development : Environments.Production
        };

        var webApplication = CreateEmptyBuilderOptions(options).Build();

        var config = Assert.IsType<ConfigurationManager>(webApplication.Configuration);
        Assert.DoesNotContain(config.Sources, source => source is JsonConfigurationSource jsonSource);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public void WebApplicationBuilderSettingInvalidApplicationDoesNotThrowWhenAssemblyLoadForUserSecretsFail(CreateBuilderOptionsFunc createBuilder)
    {
        var options = new WebApplicationOptions
        {
            ApplicationName = nameof(WebApplicationTests), // This is not a real assembly
            EnvironmentName = Environments.Development
        };

        // Use secrets fails to load an invalid assembly name but does not throw
        var webApplication = createBuilder(options).Build();

        Assert.Equal(nameof(WebApplicationTests), webApplication.Environment.ApplicationName);
        Assert.Equal(Environments.Development, webApplication.Environment.EnvironmentName);
    }

    [Theory]
    [MemberData(nameof(CreateNonEmptyBuilderOptionsFuncs))] // empty builder doesn't enable UserSecrets
    public void WebApplicationBuilderEnablesUserSecretsInDevelopment(CreateBuilderOptionsFunc createBuilder)
    {
        var options = new WebApplicationOptions
        {
            ApplicationName = typeof(WebApplicationTests).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        };

        var webApplication = createBuilder(options).Build();

        var config = Assert.IsType<ConfigurationManager>(webApplication.Configuration);
        Assert.Contains(config.Sources, source => source is JsonConfigurationSource jsonSource && jsonSource.Path == "secrets.json");
    }

    [Fact]
    public void EmptyWebApplicationBuilderDoesNotEnableUserSecretsInDevelopment()
    {
        var options = new WebApplicationOptions
        {
            ApplicationName = typeof(WebApplicationTests).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        };

        var webApplication = CreateEmptyBuilderOptions(options).Build();

        var config = Assert.IsType<ConfigurationManager>(webApplication.Configuration);
        // empty builder doesn't contain any Json sources (user secrets or otherwise) by default
        Assert.DoesNotContain(config.Sources, source => source is JsonConfigurationSource jsonSource);
    }

    [Theory]
    [MemberData(nameof(WebApplicationBuilderConstructorFuncs))]
    public void WebApplicationBuilderCanConfigureHostSettingsUsingWebApplicationOptions(WebApplicationBuilderConstructorFunc createBuilder)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRoot);
        var webRoot = "wwwroot2";
        var fullWebRootPath = Path.Combine(contentRoot, webRoot);
        var envName = $"{nameof(WebApplicationTests)}_ENV";

        try
        {
            var options = new WebApplicationOptions
            {
                ApplicationName = nameof(WebApplicationTests),
                ContentRootPath = contentRoot,
                EnvironmentName = envName,
                WebRootPath = webRoot
            };

            var builder = createBuilder(
                options,
                bootstrapBuilder =>
                {
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                        Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                        Assert.Equal(contentRoot, context.HostingEnvironment.ContentRootPath);
                    });
                });

            Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
            Assert.Equal(envName, builder.Environment.EnvironmentName);
            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
            Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(WebApplicationBuilderConstructorFuncs))]
    public void WebApplicationBuilderWebApplicationOptionsPropertiesOverridesArgs(WebApplicationBuilderConstructorFunc createBuilder)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRoot);
        var webRoot = "wwwroot2";
        var fullWebRootPath = Path.Combine(contentRoot, webRoot);
        var envName = $"{nameof(WebApplicationTests)}_ENV";

        try
        {
            var options = new WebApplicationOptions
            {
                Args = new[] {
                        $"--{WebHostDefaults.ApplicationKey}=testhost",
                        $"--{WebHostDefaults.ContentRootKey}={contentRoot}",
                        $"--{WebHostDefaults.WebRootKey}=wwwroot2",
                        $"--{WebHostDefaults.EnvironmentKey}=Test"
                    },
                ApplicationName = nameof(WebApplicationTests),
                ContentRootPath = contentRoot,
                EnvironmentName = envName,
                WebRootPath = webRoot
            };

            var builder = createBuilder(
                options,
                bootstrapBuilder =>
                {
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                        Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                        Assert.Equal(contentRoot, context.HostingEnvironment.ContentRootPath);
                    });
                });

            Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
            Assert.Equal(envName, builder.Environment.EnvironmentName);
            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
            Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(WebApplicationBuilderConstructorFuncs))]
    public void WebApplicationBuilderCanConfigureHostSettingsUsingWebApplicationOptionsArgs(WebApplicationBuilderConstructorFunc createBuilder)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRoot);
        var webRoot = "wwwroot";
        var fullWebRootPath = Path.Combine(contentRoot, webRoot);
        var envName = $"{nameof(WebApplicationTests)}_ENV";

        try
        {

            var options = new WebApplicationOptions
            {
                Args = new[] {
                        $"--{WebHostDefaults.ApplicationKey}={nameof(WebApplicationTests)}",
                        $"--{WebHostDefaults.ContentRootKey}={contentRoot}",
                        $"--{WebHostDefaults.EnvironmentKey}={envName}",
                        $"--{WebHostDefaults.WebRootKey}={webRoot}",
                    }
            };

            var builder = createBuilder(
                options,
                bootstrapBuilder =>
                {
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                        Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                        Assert.Equal(contentRoot, context.HostingEnvironment.ContentRootPath);
                    });
                });

            Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
            Assert.Equal(envName, builder.Environment.EnvironmentName);
            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
            Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(WebApplicationBuilderConstructorFuncs))]
    public void WebApplicationBuilderApplicationNameDefaultsToEntryAssembly(WebApplicationBuilderConstructorFunc createBuilder)
    {
        var assemblyName = Assembly.GetEntryAssembly().GetName().Name;

        var builder = createBuilder(
            new(),
            bootstrapBuilder =>
            {
                // Verify the defaults observed by the boostrap host builder we use internally to populate
                // the defaults
                bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                {
                    Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
                });
            });

        Assert.Equal(assemblyName, builder.Environment.ApplicationName);
        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
        });

        builder.WebHost.ConfigureAppConfiguration((context, config) =>
        {
            Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
        });

        var app = builder.Build();
        var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
        var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(assemblyName, hostEnv.ApplicationName);
        Assert.Equal(assemblyName, webHostEnv.ApplicationName);
    }

    [Theory]
    [MemberData(nameof(WebApplicationBuilderConstructorFuncs))]
    public void WebApplicationBuilderApplicationNameCanBeOverridden(WebApplicationBuilderConstructorFunc createBuilder)
    {
        var assemblyName = typeof(WebApplicationTests).Assembly.GetName().Name;

        var options = new WebApplicationOptions
        {
            ApplicationName = assemblyName
        };

        var builder = createBuilder(
            options,
            bootstrapBuilder =>
            {
                // Verify the defaults observed by the boostrap host builder we use internally to populate
                // the defaults
                bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
            {
                Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
            });
            });

        Assert.Equal(assemblyName, builder.Environment.ApplicationName);
        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
        });

        builder.WebHost.ConfigureAppConfiguration((context, config) =>
        {
            Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
        });

        var app = builder.Build();
        var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
        var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(assemblyName, hostEnv.ApplicationName);
        Assert.Equal(assemblyName, webHostEnv.ApplicationName);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void WebApplicationBuilderConfigurationSourcesOrderedCorrectly()
    {
        // all WebApplicationBuilders have the following configuration sources ordered highest to lowest priority:
        // 1. Command-line arguments
        // 2. Non-prefixed environment variables
        // 3. DOTNET_-prefixed environment variables
        // 4. ASPNETCORE_-prefixed environment variables

        var options = new RemoteInvokeOptions();
        options.StartInfo.EnvironmentVariables.Add("one", "unprefixed_one");
        options.StartInfo.EnvironmentVariables.Add("two", "unprefixed_two");
        options.StartInfo.EnvironmentVariables.Add("DOTNET_one", "DOTNET_one");
        options.StartInfo.EnvironmentVariables.Add("DOTNET_two", "DOTNET_two");
        options.StartInfo.EnvironmentVariables.Add("DOTNET_three", "DOTNET_three");
        options.StartInfo.EnvironmentVariables.Add("ASPNETCORE_one", "ASPNETCORE_one");
        options.StartInfo.EnvironmentVariables.Add("ASPNETCORE_two", "ASPNETCORE_two");
        options.StartInfo.EnvironmentVariables.Add("ASPNETCORE_three", "ASPNETCORE_three");
        options.StartInfo.EnvironmentVariables.Add("ASPNETCORE_four", "ASPNETCORE_four");

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var args = new[] { "--one=command_line_one" };
            // empty builder doesn't enable environment variable configuration by default
            foreach (object[] data in CreateNonEmptyBuilderArgsFuncs)
            {
                var createBuilder = (CreateBuilderArgsFunc)data[0];
                var builder = createBuilder(args);

                Assert.Equal("command_line_one", builder.Configuration["one"]);
                Assert.Equal("unprefixed_two", builder.Configuration["two"]);
                Assert.Equal("DOTNET_three", builder.Configuration["three"]);
                Assert.Equal("ASPNETCORE_four", builder.Configuration["four"]);
            }
        }, options);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderArgsFuncs))]
    public void WebApplicationBuilderCanFlowCommandLineConfigurationToApplication(CreateBuilderArgsFunc createBuilder)
    {
        var builder = createBuilder(new[] { "--x=1", "--name=Larry", "--age=20", "--environment=Testing" });

        Assert.Equal("1", builder.Configuration["x"]);
        Assert.Equal("Larry", builder.Configuration["name"]);
        Assert.Equal("20", builder.Configuration["age"]);
        Assert.Equal("Testing", builder.Configuration["environment"]);
        Assert.Equal("Testing", builder.Environment.EnvironmentName);

        builder.WebHost.ConfigureAppConfiguration((context, config) =>
        {
            Assert.Equal("Testing", context.HostingEnvironment.EnvironmentName);
        });

        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            Assert.Equal("Testing", context.HostingEnvironment.EnvironmentName);
        });

        var app = builder.Build();
        var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
        var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal("Testing", hostEnv.EnvironmentName);
        Assert.Equal("Testing", webHostEnv.EnvironmentName);
        Assert.Equal("1", app.Configuration["x"]);
        Assert.Equal("Larry", app.Configuration["name"]);
        Assert.Equal("20", app.Configuration["age"]);
        Assert.Equal("Testing", app.Configuration["environment"]);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilderHostBuilderSettingsThatAffectTheHostCannotBeModified(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var contentRoot = Path.GetTempPath().ToString();
        var envName = $"{nameof(WebApplicationTests)}_ENV";

        Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureHostConfiguration(builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                    { HostDefaults.ApplicationKey, "myapp" }
            });
        }));

        Assert.Throws<NotSupportedException>(() => builder.Host.UseEnvironment(envName));
        Assert.Throws<NotSupportedException>(() => builder.Host.UseContentRoot(contentRoot));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilderWebHostUseSettingCanBeReadByConfiguration(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        builder.WebHost.UseSetting("A", "value");
        builder.WebHost.UseSetting("B", "another");

        Assert.Equal("value", builder.WebHost.GetSetting("A"));
        Assert.Equal("another", builder.WebHost.GetSetting("B"));

        var app = builder.Build();

        Assert.Equal("value", app.Configuration["A"]);
        Assert.Equal("another", app.Configuration["B"]);

        Assert.Equal("value", builder.Configuration["A"]);
        Assert.Equal("another", builder.Configuration["B"]);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationCanObserveConfigurationChangesMadeInBuild(CreateBuilderFunc createBuilder)
    {
        // This mimics what WebApplicationFactory<T> does and runs configure
        // services callbacks
        using var listener = new HostingListener(hostBuilder =>
        {
            hostBuilder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                        { "A", "A" },
                        { "B", "B" },
                });
            });

            hostBuilder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                        { "C", "C" },
                        { "D", "D" },
                });
            });

            hostBuilder.ConfigureWebHost(builder =>
            {
                builder.UseSetting("E", "E");

                builder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>()
                    {
                            { "F", "F" },
                    });
                });
            });
        });

        var builder = createBuilder();

        await using var app = builder.Build();

        Assert.Equal("A", app.Configuration["A"]);
        Assert.Equal("B", app.Configuration["B"]);
        Assert.Equal("C", app.Configuration["C"]);
        Assert.Equal("D", app.Configuration["D"]);
        Assert.Equal("E", app.Configuration["E"]);
        Assert.Equal("F", app.Configuration["F"]);

        Assert.Equal("A", builder.Configuration["A"]);
        Assert.Equal("B", builder.Configuration["B"]);
        Assert.Equal("C", builder.Configuration["C"]);
        Assert.Equal("D", builder.Configuration["D"]);
        Assert.Equal("E", builder.Configuration["E"]);
        Assert.Equal("F", builder.Configuration["F"]);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationCanObserveSourcesClearedInBuild(CreateBuilderFunc createBuilder)
    {
        // This mimics what WebApplicationFactory<T> does and runs configure
        // services callbacks
        using var listener = new HostingListener(hostBuilder =>
        {
            hostBuilder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "A", "A" },
                });
            });

            hostBuilder.ConfigureAppConfiguration(config =>
            {
                // This clears configuration added both via ConfigureHostConfiguration and builder.Configuration.
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "B", "B" },
                });
            });
        });

        var builder = createBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "C", "C" },
            });

        await using var app = builder.Build();

        Assert.True(string.IsNullOrEmpty(app.Configuration["A"]));
        Assert.True(string.IsNullOrEmpty(app.Configuration["C"]));

        Assert.Equal("B", app.Configuration["B"]);

        Assert.Same(builder.Configuration, app.Configuration);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public async Task WebApplicationCanObserveSourcesClearedInHostConfiguration(CreateBuilderOptionsFunc createBuilder)
    {
        // This mimics what WebApplicationFactory<T> does and runs configure
        // services callbacks
        using var listener = new HostingListener(hostBuilder =>
        {
            hostBuilder.ConfigureHostConfiguration(config =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    // Make sure we don't change host defaults
                    { HostDefaults.ApplicationKey, "appName" },
                    { HostDefaults.EnvironmentKey, "environmentName" },
                    { HostDefaults.ContentRootKey, Directory.GetCurrentDirectory() },
                    { "A", "A" },
                });
            });
        });

        var builder = createBuilder(new WebApplicationOptions
        {
            ApplicationName = "appName",
            EnvironmentName = "environmentName",
            ContentRootPath = Directory.GetCurrentDirectory(),
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "B", "B" },
        });

        await using var app = builder.Build();

        Assert.True(string.IsNullOrEmpty(app.Configuration["B"]));

        Assert.Equal("A", app.Configuration["A"]);

        Assert.Same(builder.Configuration, app.Configuration);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationCanHandleStreamBackedConfigurationAddedInBuild(CreateBuilderFunc createBuilder)
    {
        static Stream CreateStreamFromString(string data) => new MemoryStream(Encoding.UTF8.GetBytes(data));

        using var jsonAStream = CreateStreamFromString(@"{ ""A"": ""A"" }");
        using var jsonBStream = CreateStreamFromString(@"{ ""B"": ""B"" }");

        // This mimics what WebApplicationFactory<T> does and runs configure
        // services callbacks
        using var listener = new HostingListener(hostBuilder =>
        {
            hostBuilder.ConfigureHostConfiguration(config => config.AddJsonStream(jsonAStream));
            hostBuilder.ConfigureAppConfiguration(config => config.AddJsonStream(jsonBStream));
        });

        var builder = createBuilder();
        await using var app = builder.Build();

        Assert.Equal("A", app.Configuration["A"]);
        Assert.Equal("B", app.Configuration["B"]);

        Assert.Same(builder.Configuration, app.Configuration);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationDisposesConfigurationProvidersAddedInBuild(CreateBuilderFunc createBuilder)
    {
        var hostConfigSource = new RandomConfigurationSource();
        var appConfigSource = new RandomConfigurationSource();

        // This mimics what WebApplicationFactory<T> does and runs configure
        // services callbacks
        using var listener = new HostingListener(hostBuilder =>
        {
            hostBuilder.ConfigureHostConfiguration(config => config.Add(hostConfigSource));
            hostBuilder.ConfigureAppConfiguration(config => config.Add(appConfigSource));
        });

        var builder = createBuilder();

        {
            await using var app = builder.Build();

            Assert.Equal(1, hostConfigSource.ProvidersBuilt);
            Assert.Equal(1, appConfigSource.ProvidersBuilt);
            Assert.Equal(1, hostConfigSource.ProvidersLoaded);
            Assert.Equal(1, appConfigSource.ProvidersLoaded);
            Assert.Equal(0, hostConfigSource.ProvidersDisposed);
            Assert.Equal(0, appConfigSource.ProvidersDisposed);
        }

        Assert.Equal(1, hostConfigSource.ProvidersBuilt);
        Assert.Equal(1, appConfigSource.ProvidersBuilt);
        Assert.Equal(1, hostConfigSource.ProvidersLoaded);
        Assert.Equal(1, appConfigSource.ProvidersLoaded);
        Assert.Equal(1, hostConfigSource.ProvidersDisposed);
        Assert.Equal(1, appConfigSource.ProvidersDisposed);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationMakesOriginalConfigurationProvidersAddedInBuildAccessable(CreateBuilderFunc createBuilder)
    {
        // This mimics what WebApplicationFactory<T> does and runs configure
        // services callbacks
        using var listener = new HostingListener(hostBuilder =>
        {
            hostBuilder.ConfigureAppConfiguration(config => config.Add(new RandomConfigurationSource()));
        });

        var builder = createBuilder();
        await using var app = builder.Build();

        Assert.Single(((IConfigurationRoot)app.Configuration).Providers.OfType<RandomConfigurationProvider>());
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilderHostProperties_IsCaseSensitive(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        builder.Host.Properties["lowercase"] = nameof(WebApplicationTests);

        Assert.Equal(nameof(WebApplicationTests), builder.Host.Properties["lowercase"]);
        Assert.False(builder.Host.Properties.ContainsKey("Lowercase"));
    }

    [Theory]
    [MemberData(nameof(CreateNonEmptyBuilderFuncs))] // empty builder doesn't enable HostFiltering
    public async Task WebApplicationConfiguration_HostFilterOptionsAreReloadable(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var host = builder.WebHost
            .ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.Add(new ReloadableMemorySource());
            });
        await using var app = builder.Build();

        var config = app.Services.GetRequiredService<IConfiguration>();
        var monitor = app.Services.GetRequiredService<IOptionsMonitor<HostFilteringOptions>>();
        var options = monitor.CurrentValue;

        Assert.Contains("*", options.AllowedHosts);

        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.OnChange(newOptions =>
        {
            changed.TrySetResult();
        });

        config["AllowedHosts"] = "NewHost";

        await changed.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        options = monitor.CurrentValue;
        Assert.Contains("NewHost", options.AllowedHosts);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void CanResolveIConfigurationBeforeBuildingApplication(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var sp = builder.Services.BuildServiceProvider();

        var config = sp.GetService<IConfiguration>();
        Assert.NotNull(config);
        Assert.Same(config, builder.Configuration);

        var app = builder.Build();

        Assert.Same(app.Configuration, builder.Configuration);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ManuallyAddingConfigurationAsServiceWorks(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
        var sp = builder.Services.BuildServiceProvider();

        var config = sp.GetService<IConfiguration>();
        Assert.NotNull(config);
        Assert.Same(config, builder.Configuration);

        var app = builder.Build();

        Assert.Same(app.Configuration, builder.Configuration);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void AddingMemoryStreamBackedConfigurationWorks(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var jsonConfig = @"{ ""foo"": ""bar"" }";
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        sw.WriteLine(jsonConfig);
        sw.Flush();

        ms.Position = 0;
        builder.Configuration.AddJsonStream(ms);

        Assert.Equal("bar", builder.Configuration["foo"]);

        var app = builder.Build();

        Assert.Equal("bar", app.Configuration["foo"]);
    }

    [Theory]
    [MemberData(nameof(CreateNonEmptyBuilderFuncs))] // empty builder doesn't enable ForwardedHeaders
    public async Task WebApplicationConfiguration_EnablesForwardedHeadersFromConfig(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["FORWARDEDHEADERS_ENABLED"] = "true";
        await using var app = builder.Build();

        app.Run(context =>
        {
            Assert.Equal("https", context.Request.Scheme);
            return Task.CompletedTask;
        });

        await app.StartAsync();

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-forwarded-proto", "https");
        var result = await client.GetAsync("http://localhost/");
        result.EnsureSuccessStatusCode();
    }

    [Fact]
    public void WebApplicationCreate_RegistersRouting()
    {
        var app = WebApplication.Create();
        var linkGenerator = app.Services.GetService(typeof(LinkGenerator));
        Assert.NotNull(linkGenerator);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplication_CanResolveDefaultServicesFromServiceCollection(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        // Add the service collection to the service collection
        builder.Services.AddSingleton(builder.Services);

        var app = builder.Build();

        var env0 = app.Services.GetRequiredService<IHostEnvironment>();

        var env1 = app.Services.GetRequiredService<IServiceCollection>().BuildServiceProvider().GetRequiredService<IHostEnvironment>();

        Assert.Equal(env0.ApplicationName, env1.ApplicationName);
        Assert.Equal(env0.EnvironmentName, env1.EnvironmentName);
        Assert.Equal(env0.ContentRootPath, env1.ContentRootPath);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplication_CanResolveServicesAddedAfterBuildFromServiceCollection(CreateBuilderFunc createBuilder)
    {
        // This mimics what WebApplicationFactory<T> does and runs configure
        // services callbacks
        using var listener = new HostingListener(hostBuilder =>
        {
            hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IService, Service>();
            });
        });

        var builder = createBuilder();

        // Add the service collection to the service collection
        builder.Services.AddSingleton(builder.Services);

        await using var app = builder.Build();

        var service0 = app.Services.GetRequiredService<IService>();

        var service1 = app.Services.GetRequiredService<IServiceCollection>().BuildServiceProvider().GetRequiredService<IService>();

        Assert.IsType<Service>(service0);
        Assert.IsType<Service>(service1);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplication_CanResolveIConfigurationFromServiceCollection(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["foo"] = "bar",
        });

        Assert.Equal("bar", builder.Configuration["foo"]);

        // NOTE: This prevents HostFactoryResolver from adding any new configuration sources since these
        // are added during builder.Build().
        using (var serviceProvider = builder.Services.BuildServiceProvider())
        {
            var config = serviceProvider.GetService<IConfiguration>();

            Assert.Equal("bar", config["foo"]);
        }

        await using var app = builder.Build();

        Assert.Equal("bar", app.Configuration["foo"]);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplication_CanResolveDefaultServicesFromServiceCollectionInCorrectOrder(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        // Add the service collection to the service collection
        builder.Services.AddSingleton(builder.Services);

        // We're overriding the default IHostLifetime so that we can test the order in which it's resolved.
        // This should override the default IHostLifetime.
        builder.Services.AddSingleton<IHostLifetime, CustomHostLifetime>();

        var app = builder.Build();

        var hostLifetime0 = app.Services.GetRequiredService<IHostLifetime>();
        var childServiceProvider = app.Services.GetRequiredService<IServiceCollection>().BuildServiceProvider();
        var hostLifetime1 = childServiceProvider.GetRequiredService<IHostLifetime>();

        var hostLifetimes0 = app.Services.GetServices<IHostLifetime>().ToArray();
        var hostLifetimes1 = childServiceProvider.GetServices<IHostLifetime>().ToArray();

        Assert.IsType<CustomHostLifetime>(hostLifetime0);
        Assert.IsType<CustomHostLifetime>(hostLifetime1);

        Assert.Equal(hostLifetimes1.Length, hostLifetimes0.Length);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplication_CanCallUseRoutingWithoutUseEndpoints(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.MapGet("/new", () => "new");

        // Rewrite "/old" to "/new" before matching routes
        app.Use((context, next) =>
        {
            if (context.Request.Path == "/old")
            {
                context.Request.Path = "/new";
            }

            return next(context);
        });

        app.UseRouting();

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();

        var newEndpoint = Assert.Single(endpointDataSource.Endpoints);
        var newRouteEndpoint = Assert.IsType<RouteEndpoint>(newEndpoint);
        Assert.Equal("/new", newRouteEndpoint.RoutePattern.RawText);

        var client = app.GetTestClient();

        var oldResult = await client.GetAsync("http://localhost/old");
        oldResult.EnsureSuccessStatusCode();

        Assert.Equal("new", await oldResult.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplication_CanCallUseEndpointsWithoutUseRoutingFails(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.MapGet("/1", () => "1");

        var ex = Assert.Throws<InvalidOperationException>(() => app.UseEndpoints(endpoints => { }));
        Assert.Contains("UseRouting", ex.Message);
    }

    [Fact]
    public void WebApplicationCreate_RegistersEventSourceLogger()
    {
        using var listener = new TestEventListener();
        var app = WebApplication.Create();

        var logger = app.Services.GetRequiredService<ILogger<WebApplicationTests>>();
        var guid = Guid.NewGuid().ToString();
        logger.LogInformation(guid);

        var events = listener.EventData.ToArray();
        Assert.Contains(events, args =>
            args.EventSource.Name == "Microsoft-Extensions-Logging" &&
            args.Payload.OfType<string>().Any(p => p.Contains(guid)));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilder_CanClearDefaultLoggers(CreateBuilderFunc createBuilder)
    {
        using var listener = new TestEventListener();
        var builder = createBuilder();
        builder.Logging.ClearProviders();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<WebApplicationTests>>();
        var guid = Guid.NewGuid().ToString();
        logger.LogInformation(guid);

        var events = listener.EventData.ToArray();
        Assert.DoesNotContain(events, args =>
            args.EventSource.Name == "Microsoft-Extensions-Logging" &&
            args.Payload.OfType<string>().Any(p => p.Contains(guid)));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationBuilder_StartupFilterCanAddTerminalMiddleware(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IStartupFilter, TerminalMiddlewareStartupFilter>();
        await using var app = builder.Build();

        app.MapGet("/defined", () => { });

        await app.StartAsync();

        var client = app.GetTestClient();

        var definedResult = await client.GetAsync("http://localhost/defined");
        definedResult.EnsureSuccessStatusCode();

        var terminalResult = await client.GetAsync("http://localhost/undefined");
        Assert.Equal(418, (int)terminalResult.StatusCode);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task StartupFilter_WithUseRoutingWorks(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IStartupFilter, UseRoutingStartupFilter>();
        await using var app = builder.Build();

        var chosenEndpoint = string.Empty;
        app.MapGet("/", async c =>
        {
            chosenEndpoint = c.GetEndpoint().DisplayName;
            await c.Response.WriteAsync("Hello World");
        }).WithDisplayName("One");

        await app.StartAsync();

        var client = app.GetTestClient();

        _ = await client.GetAsync("http://localhost/");
        Assert.Equal("One", chosenEndpoint);

        var response = await client.GetAsync("http://localhost/1");
        Assert.Equal(203, ((int)response.StatusCode));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task CanAddMiddlewareBeforeUseRouting(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        var chosenEndpoint = string.Empty;

        app.Use((c, n) =>
        {
            chosenEndpoint = c.GetEndpoint()?.DisplayName;
            Assert.Null(c.GetEndpoint());
            return n(c);
        });

        app.UseRouting();

        app.MapGet("/1", async c =>
        {
            chosenEndpoint = c.GetEndpoint().DisplayName;
            await c.Response.WriteAsync("Hello World");
        }).WithDisplayName("One");

        app.UseEndpoints(e => { });

        await app.StartAsync();

        var client = app.GetTestClient();

        _ = await client.GetAsync("http://localhost/");
        Assert.Null(chosenEndpoint);

        _ = await client.GetAsync("http://localhost/1");
        Assert.Equal("One", chosenEndpoint);
    }

    [Theory]
    [MemberData(nameof(CreateNonEmptyBuilderFuncs))]
    public async Task WebApplicationBuilder_OnlyAddsDefaultServicesOnce(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<LoggerFactoryOptions>)));
        // IWebHostEnvironment is added by ConfigureWebHostDefaults
        Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IWebHostEnvironment)));
        Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IOptionsChangeTokenSource<HostFilteringOptions>)));
        Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IServer)));
        Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(EndpointDataSource)));

        await using var app = builder.Build();

        Assert.Single(app.Services.GetRequiredService<IEnumerable<IConfigureOptions<LoggerFactoryOptions>>>());
        Assert.Single(app.Services.GetRequiredService<IEnumerable<IWebHostEnvironment>>());
        Assert.Single(app.Services.GetRequiredService<IEnumerable<IOptionsChangeTokenSource<HostFilteringOptions>>>());
        Assert.Single(app.Services.GetRequiredService<IEnumerable<IServer>>());
    }

    [Fact]
    public void EmptyWebApplicationBuilder_OnlyContainsMinimalServices()
    {
        var builder = WebApplication.CreateEmptyBuilder(new());

        Assert.Empty(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<LoggerFactoryOptions>)));
        Assert.Empty(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IOptionsChangeTokenSource<HostFilteringOptions>)));
        Assert.Empty(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IServer)));
        Assert.Empty(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(EndpointDataSource)));

        // These services are still necessary
        Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IWebHostEnvironment)));
    }

    [Theory]
    [MemberData(nameof(CreateNonEmptyBuilderArgsFuncs))] // empty builder doesn't enable DI validation
    public void WebApplicationBuilder_EnablesServiceScopeValidationByDefaultInDevelopment(CreateBuilderArgsFunc createBuilder)
    {
        // The environment cannot be reconfigured after the builder is created currently.
        var builder = createBuilder(new[] { "--environment", "Development" });

        builder.Services.AddScoped<Service>();
        builder.Services.AddSingleton<Service2>();

        // This currently throws an AggregateException, but any Exception from Build() is enough to make this test pass.
        // If this is throwing for any reason other than service scope validation, we'll likely see it in other tests.
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Fact]
    public void EmptyWebApplicationBuilder_DoesNotEnableServiceScopeValidationByDefaultInDevelopment()
    {
        // The environment cannot be reconfigured after the builder is created currently.
        var builder = CreateEmptyBuilderArgs(new[] { "--environment", "Development" });

        builder.Services.AddScoped<Service>();
        builder.Services.AddSingleton<Service2>();

        // This shouldn't throw at all since DI validation is not enabled
        Assert.NotNull(builder.Build());
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplicationBuilder_ThrowsExceptionIfServicesAlreadyBuilt(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        await using var app = builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.Services.AddSingleton<IService>(new Service()));
        Assert.Throws<InvalidOperationException>(() => builder.Services.TryAddSingleton(new Service()));
        Assert.Throws<InvalidOperationException>(() => builder.Services.AddScoped<IService, Service>());
        Assert.Throws<InvalidOperationException>(() => builder.Services.TryAddScoped<IService, Service>());
        Assert.Throws<InvalidOperationException>(() => builder.Services.Remove(ServiceDescriptor.Singleton(new Service())));
        Assert.Throws<InvalidOperationException>(() => builder.Services[0] = ServiceDescriptor.Singleton(new Service()));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void WebApplicationBuilder_ThrowsFromExtensionMethodsNotSupportedByHostAndWebHost(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var ex = Assert.Throws<NotSupportedException>(() => builder.WebHost.Configure(app => { }));
        var ex1 = Assert.Throws<NotSupportedException>(() => builder.WebHost.Configure((context, app) => { }));
        var ex2 = Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup<MyStartup>());
        var ex3 = Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup(typeof(MyStartup)));
        var ex4 = Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup(context => new MyStartup()));

        Assert.Equal("Configure() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex.Message);
        Assert.Equal("Configure() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex1.Message);
        Assert.Equal("UseStartup() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex2.Message);
        Assert.Equal("UseStartup() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex3.Message);
        Assert.Equal("UseStartup() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex4.Message);

        var ex5 = Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHost(webHostBuilder => { }));
        var ex6 = Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHost(webHostBuilder => { }, options => { }));
        var ex7 = Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureSlimWebHost(webHostBuilder => { }, options => { }));
        var ex8 = Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHostDefaults(webHostBuilder => { }));

        Assert.Equal("ConfigureWebHost() is not supported by WebApplicationBuilder.Host. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex5.Message);
        Assert.Equal("ConfigureWebHost() is not supported by WebApplicationBuilder.Host. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex6.Message);
        Assert.Equal("ConfigureWebHost() is not supported by WebApplicationBuilder.Host. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex7.Message);
        Assert.Equal("ConfigureWebHost() is not supported by WebApplicationBuilder.Host. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex8.Message);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task EndpointDataSourceOnlyAddsOnce(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UseRouting();

        app.MapGet("/", () => "Hello World!").WithDisplayName("One");

        app.UseEndpoints(routes =>
        {
            routes.MapGet("/hi", () => "Hi World").WithDisplayName("Two");
            routes.MapGet("/heyo", () => "Heyo World").WithDisplayName("Three");
        });

        app.Start();

        var ds = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Equal(3, ds.Endpoints.Count);
        Assert.Equal("One", ds.Endpoints[0].DisplayName);
        Assert.Equal("Two", ds.Endpoints[1].DisplayName);
        Assert.Equal("Three", ds.Endpoints[2].DisplayName);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task RoutesAddedToCorrectMatcher(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UseRouting();

        var chosenRoute = string.Empty;

        app.Use((context, next) =>
        {
            chosenRoute = context.GetEndpoint()?.DisplayName;
            return next(context);
        });

        app.MapGet("/", () => "Hello World").WithDisplayName("One");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/hi", () => "Hello Endpoints").WithDisplayName("Two");
        });

        app.UseRouting();
        app.UseEndpoints(_ => { });

        await app.StartAsync();

        var client = app.GetTestClient();

        _ = await client.GetAsync("http://localhost/");
        Assert.Equal("One", chosenRoute);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task WebApplication_CallsUseRoutingAndUseEndpoints(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        var chosenRoute = string.Empty;
        app.MapGet("/", async c =>
        {
            chosenRoute = c.GetEndpoint()?.DisplayName;
            await c.Response.WriteAsync("Hello World");
        }).WithDisplayName("One");

        await app.StartAsync();

        var ds = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Equal(1, ds.Endpoints.Count);
        Assert.Equal("One", ds.Endpoints[0].DisplayName);

        var client = app.GetTestClient();

        _ = await client.GetAsync("http://localhost/");
        Assert.Equal("One", chosenRoute);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task BranchingPipelineHasOwnRoutes(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UseRouting();

        var chosenRoute = string.Empty;
        app.MapGet("/", () => "Hello World!").WithDisplayName("One");

        app.UseEndpoints(routes =>
        {
            routes.MapGet("/hi", async c =>
            {
                chosenRoute = c.GetEndpoint()?.DisplayName;
                await c.Response.WriteAsync("Hello World");
            }).WithDisplayName("Two");
            routes.MapGet("/heyo", () => "Heyo World").WithDisplayName("Three");
        });

        var newBuilder = ((IApplicationBuilder)app).New();
        Assert.False(newBuilder.Properties.TryGetValue(WebApplication.GlobalEndpointRouteBuilderKey, out _));

        newBuilder.UseRouting();
        newBuilder.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/h3", async c =>
            {
                chosenRoute = c.GetEndpoint()?.DisplayName;
                await c.Response.WriteAsync("Hello World");
            }).WithDisplayName("Four");
            endpoints.MapGet("hi", async c =>
            {
                chosenRoute = c.GetEndpoint()?.DisplayName;
                await c.Response.WriteAsync("Hi New");
            }).WithDisplayName("Five");
        });
        var branch = newBuilder.Build();
        app.Run(c => branch(c));

        app.Start();

        var ds = app.Services.GetRequiredService<EndpointDataSource>();
        var displayNames = ds.Endpoints.Select(e => e.DisplayName).ToArray();
        Assert.Equal(5, displayNames.Length);
        Assert.Contains("One", displayNames);
        Assert.Contains("Two", displayNames);
        Assert.Contains("Three", displayNames);
        Assert.Contains("Four", displayNames);
        Assert.Contains("Five", displayNames);

        var client = app.GetTestClient();

        // '/hi' routes don't conflict and the non-branched one is chosen
        _ = await client.GetAsync("http://localhost/hi");
        Assert.Equal("Two", chosenRoute);

        // Can access branched routes
        _ = await client.GetAsync("http://localhost/h3");
        Assert.Equal("Four", chosenRoute);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task PropertiesPreservedFromInnerApplication(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IStartupFilter, PropertyFilter>();
        await using var app = builder.Build();

        ((IApplicationBuilder)app).Properties["didsomething"] = true;

        app.Start();
    }

    [Theory]
    [MemberData(nameof(CreateNonEmptyBuilderOptionsFuncs))] // empty builder doesn't enable the DeveloperExceptionPage
    public async Task DeveloperExceptionPageIsOnByDefaultInDevelopment(CreateBuilderOptionsFunc createBuilder)
    {
        var builder = createBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.MapGet("/", void () => throw new InvalidOperationException("BOOM"));

        await app.StartAsync();

        var client = app.GetTestClient();

        var response = await client.GetAsync("/");

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("BOOM", await response.Content.ReadAsStringAsync());
        Assert.Contains("text/plain", response.Content.Headers.ContentType.MediaType);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public async Task DeveloperExceptionPageDoesNotGetCaughtByStartupFilters(CreateBuilderOptionsFunc createBuilder)
    {
        var builder = createBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IStartupFilter, ThrowingStartupFilter>();
        await using var app = builder.Build();

        await app.StartAsync();

        var client = app.GetTestClient();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/"));

        Assert.Equal("BOOM Filter", ex.Message);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public async Task DeveloperExceptionPageIsNotOnInProduction(CreateBuilderOptionsFunc createBuilder)
    {
        var builder = createBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Production });
        await DeveloperExceptionPageIsNotOn(builder);
    }

    [Fact]
    public async Task DeveloperExceptionPageIsNotOnInDevelopmentWithEmptyBuilder()
    {
        var builder = CreateEmptyBuilderOptions(new WebApplicationOptions() { EnvironmentName = Environments.Development });
        await DeveloperExceptionPageIsNotOn(builder);
    }

    private async Task DeveloperExceptionPageIsNotOn(WebApplicationBuilder builder)
    {
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.MapGet("/", void () => throw new InvalidOperationException("BOOM"));

        await app.StartAsync();

        var client = app.GetTestClient();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/"));

        Assert.Equal("BOOM", ex.Message);
    }

    [Fact]
    public async Task HostingStartupRunsWhenApplicationIsNotEntryPoint()
    {
        // NOTE: CreateSlimBuilder doesn't support Startups
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(WebApplicationTests).Assembly.FullName });
        await using var app = builder.Build();

        Assert.Equal("value", app.Configuration["testhostingstartup:config"]);
    }

    [Fact]
    public async Task HostingStartupRunsWhenApplicationIsNotEntryPointWithArgs()
    {
        // NOTE: CreateSlimBuilder doesn't support Startups
        var builder = WebApplication.CreateBuilder(new[] { "--applicationName", typeof(WebApplicationTests).Assembly.FullName });
        await using var app = builder.Build();

        Assert.Equal("value", app.Configuration["testhostingstartup:config"]);
    }

    [Fact]
    public async Task HostingStartupRunsWhenApplicationIsNotEntryPointApplicationNameWinsOverArgs()
    {
        var options = new WebApplicationOptions
        {
            Args = new[] { "--applicationName", typeof(WebApplication).Assembly.FullName },
            ApplicationName = typeof(WebApplicationTests).Assembly.FullName,
        };
        // NOTE: CreateSlimBuilder doesn't support Startups
        var builder = WebApplication.CreateBuilder(options);
        await using var app = builder.Build();

        Assert.Equal("value", app.Configuration["testhostingstartup:config"]);
    }

    [Theory]
    [MemberData(nameof(CreateNonEmptyBuilderOptionsFuncs))] // empty builder doesn't enable the DeveloperExceptionPage
    public async Task DeveloperExceptionPageWritesBadRequestDetailsToResponseByDefaultInDevelopment(CreateBuilderOptionsFunc createBuilder)
    {
        var builder = createBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.MapGet("/{parameterName}", (int parameterName) => { });

        await app.StartAsync();

        var client = app.GetTestClient();

        var response = await client.GetAsync("/notAnInt");

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("text/plain", response.Content.Headers.ContentType.MediaType);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("parameterName", responseBody);
        Assert.Contains("notAnInt", responseBody);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public async Task NoExceptionAreThrownForBadRequestsInProduction(CreateBuilderOptionsFunc createBuilder)
    {
        var builder = createBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Production });
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.MapGet("/{parameterName}", (int parameterName) => { });

        await app.StartAsync();

        var client = app.GetTestClient();

        var response = await client.GetAsync("/notAnInt");

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(string.Empty, responseBody);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void PropertiesArePropagated(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.Host.Properties["hello"] = "world";
        var callbacks = 0;

        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            callbacks |= 0b00000001;
            Assert.Equal("world", context.Properties["hello"]);
        });

        builder.Host.ConfigureServices((context, config) =>
        {
            callbacks |= 0b00000010;
            Assert.Equal("world", context.Properties["hello"]);
        });

        builder.Host.ConfigureContainer<IServiceCollection>((context, config) =>
        {
            callbacks |= 0b00000100;
            Assert.Equal("world", context.Properties["hello"]);
        });

        using var app = builder.Build();

        // Make sure all of the callbacks ran
        Assert.Equal(0b00000111, callbacks);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void EmptyAppConfiguration(CreateBuilderFunc createBuilder)
    {
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        bool createdDirectory = false;
        if (!Directory.Exists(wwwroot))
        {
            createdDirectory = true;
            Directory.CreateDirectory(wwwroot);
        }

        try
        {
            var builder = createBuilder();

            builder.WebHost.ConfigureAppConfiguration((ctx, config) => { });

            using var app = builder.Build();
            var hostEnv = app.Services.GetRequiredService<Hosting.IWebHostEnvironment>();
            Assert.Equal(wwwroot, hostEnv.WebRootPath);
        }
        finally
        {
            if (createdDirectory)
            {
                Directory.Delete(wwwroot);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void HostConfigurationNotAffectedByConfiguration(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var envName = $"{nameof(WebApplicationTests)}_ENV";

        builder.Configuration[WebHostDefaults.ApplicationKey] = nameof(WebApplicationTests);
        builder.Configuration[WebHostDefaults.EnvironmentKey] = envName;
        builder.Configuration[WebHostDefaults.ContentRootKey] = contentRoot;

        var app = builder.Build();
        var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
        var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(builder.Environment.ApplicationName, hostEnv.ApplicationName);
        Assert.Equal(builder.Environment.EnvironmentName, hostEnv.EnvironmentName);
        Assert.Equal(builder.Environment.ContentRootPath, hostEnv.ContentRootPath);

        Assert.Equal(webHostEnv.ApplicationName, hostEnv.ApplicationName);
        Assert.Equal(webHostEnv.EnvironmentName, hostEnv.EnvironmentName);
        Assert.Equal(webHostEnv.ContentRootPath, hostEnv.ContentRootPath);

        Assert.NotEqual(nameof(WebApplicationTests), hostEnv.ApplicationName);
        Assert.NotEqual(envName, hostEnv.EnvironmentName);
        Assert.NotEqual(contentRoot, hostEnv.ContentRootPath);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderOptionsFuncs))]
    public void ClearingConfigurationDoesNotAffectHostConfiguration(CreateBuilderOptionsFunc createBuilder)
    {
        var builder = createBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(WebApplicationOptions).Assembly.FullName,
            EnvironmentName = Environments.Staging,
            ContentRootPath = Path.GetTempPath()
        });

        ((IConfigurationBuilder)builder.Configuration).Sources.Clear();

        var app = builder.Build();
        var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
        var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(builder.Environment.ApplicationName, hostEnv.ApplicationName);
        Assert.Equal(builder.Environment.EnvironmentName, hostEnv.EnvironmentName);
        Assert.Equal(builder.Environment.ContentRootPath, hostEnv.ContentRootPath);

        Assert.Equal(webHostEnv.ApplicationName, hostEnv.ApplicationName);
        Assert.Equal(webHostEnv.EnvironmentName, hostEnv.EnvironmentName);
        Assert.Equal(webHostEnv.ContentRootPath, hostEnv.ContentRootPath);

        Assert.Equal(typeof(WebApplicationOptions).Assembly.FullName, hostEnv.ApplicationName);
        Assert.Equal(Environments.Staging, hostEnv.EnvironmentName);
        Assert.Equal(Path.GetTempPath(), hostEnv.ContentRootPath);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ConfigurationGetDebugViewWorks(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["foo"] = "bar",
        });

        var app = builder.Build();

        // Make sure we don't lose "MemoryConfigurationProvider" from GetDebugView() when wrapping the provider.
        Assert.Contains("foo=bar (MemoryConfigurationProvider)", ((IConfigurationRoot)app.Configuration).GetDebugView());
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ConfigurationCanBeReloaded(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        ((IConfigurationBuilder)builder.Configuration).Sources.Add(new RandomConfigurationSource());

        var app = builder.Build();

        var value0 = app.Configuration["Random"];
        ((IConfigurationRoot)app.Configuration).Reload();
        var value1 = app.Configuration["Random"];

        Assert.NotEqual(value0, value1);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ConfigurationSourcesAreBuiltOnce(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var configSource = new RandomConfigurationSource();
        ((IConfigurationBuilder)builder.Configuration).Sources.Add(configSource);

        var app = builder.Build();

        Assert.Equal(1, configSource.ProvidersBuilt);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ConfigurationProvidersAreLoadedOnceAfterBuild(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var configSource = new RandomConfigurationSource();
        ((IConfigurationBuilder)builder.Configuration).Sources.Add(configSource);

        using var app = builder.Build();

        Assert.Equal(1, configSource.ProvidersLoaded);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ConfigurationProvidersAreDisposedWithWebApplication(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        var configSource = new RandomConfigurationSource();
        ((IConfigurationBuilder)builder.Configuration).Sources.Add(configSource);

        {
            using var app = builder.Build();

            Assert.Equal(0, configSource.ProvidersDisposed);
        }

        Assert.Equal(1, configSource.ProvidersDisposed);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ConfigurationProviderTypesArePreserved(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        ((IConfigurationBuilder)builder.Configuration).Sources.Add(new RandomConfigurationSource());

        var app = builder.Build();

        Assert.Single(((IConfigurationRoot)app.Configuration).Providers.OfType<RandomConfigurationProvider>());
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task CanUseMiddleware(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.Use(next =>
        {
            return context => context.Response.WriteAsync("Hello World");
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        var response = await client.GetStringAsync("/");
        Assert.Equal("Hello World", response);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void CanObserveDefaultServicesInServiceCollection(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();

        Assert.Contains(builder.Services, service => service.ServiceType == typeof(HostBuilderContext));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IHostApplicationLifetime));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IHostLifetime));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(IOptions<>));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(ILoggerFactory));
        Assert.Contains(builder.Services, service => service.ServiceType == typeof(ILogger<>));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task RegisterAuthMiddlewaresCorrectly(CreateBuilderFunc createBuilder)
    {
        var helloEndpointCalled = false;
        var customMiddlewareExecuted = false;
        var username = "foobar";

        var builder = createBuilder();

        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication("testSchemeName")
            .AddScheme<AuthenticationSchemeOptions, UberHandler>("testSchemeName", "testDisplayName", _ => { });
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.Use(next =>
        {
            return async context =>
            {
                // IAuthenticationFeature is added by the authentication middleware
                // during invocation. This middleware should run after authentication
                // and be able to access the feature.
                var authFeature = context.Features.Get<IAuthenticationFeature>();
                Assert.NotNull(authFeature);
                customMiddlewareExecuted = true;
                Assert.Equal(username, context.User.Identity.Name);
                await next(context);
            };
        });

        app.MapGet("/hello", (ClaimsPrincipal user) =>
        {
            helloEndpointCalled = true;
            Assert.Equal(username, user.Identity.Name);
        }).AllowAnonymous();

        await app.StartAsync();
        var client = app.GetTestClient();
        await client.GetStringAsync($"/hello?username={username}");

        Assert.True(helloEndpointCalled);
        Assert.True(customMiddlewareExecuted);
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public async Task SupportsDisablingMiddlewareAutoRegistration(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication("testSchemeName")
            .AddScheme<AuthenticationSchemeOptions, UberHandler>("testSchemeName", "testDisplayName", _ => { });
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.Use(next =>
        {
            return async context =>
            {
                // IAuthenticationFeature is added by the authentication middleware
                // during invocation. This middleware should run after authentication
                // and be able to access the feature.
                var authFeature = context.Features.Get<IAuthenticationFeature>();
                Assert.Null(authFeature);
                Assert.Null(context.User.Identity.Name);
                await next(context);
            };
        });

        app.Properties["__AuthenticationMiddlewareSet"] = true;

        app.MapGet("/hello", (ClaimsPrincipal user) => { }).AllowAnonymous();

        Assert.True(app.Properties.ContainsKey("__AuthenticationMiddlewareSet"));
        Assert.False(app.Properties.ContainsKey("__AuthorizationMiddlewareSet"));

        await app.StartAsync();

        Assert.True(app.Properties.ContainsKey("__AuthenticationMiddlewareSet"));
        Assert.True(app.Properties.ContainsKey("__AuthorizationMiddlewareSet"));
    }

    [Theory]
    [MemberData(nameof(CreateBuilderFuncs))]
    public void ImplementsIHostApplicationBuilderCorrectly(CreateBuilderFunc createBuilder)
    {
        var builder = createBuilder();
        var iHostApplicationBuilder = (IHostApplicationBuilder)builder;

        builder.Host.Properties["MyProp"] = 1;
        Assert.Equal(1, iHostApplicationBuilder.Properties["MyProp"]);

        Assert.Same(builder.Host.Properties, iHostApplicationBuilder.Properties);
        Assert.Same(builder.Configuration, iHostApplicationBuilder.Configuration);
        Assert.Same(builder.Logging, iHostApplicationBuilder.Logging);
        Assert.Same(builder.Services, iHostApplicationBuilder.Services);
        Assert.True(iHostApplicationBuilder.Environment.IsProduction());
        Assert.NotNull(iHostApplicationBuilder.Environment.ContentRootFileProvider);

        iHostApplicationBuilder.ConfigureContainer(new MyServiceProviderFactory());

        var app = builder.Build();
        Assert.IsType<MyServiceProvider>(app.Services);
    }

    [Fact]
    public async Task UsingCreateBuilderResultsInRegexConstraintBeingPresent()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        var chosenRoute = string.Empty;

        app.Use((context, next) =>
        {
            chosenRoute = context.GetEndpoint()?.DisplayName;
            return next(context);
        });

        app.MapGet("/products/{productId:regex(^[a-z]{{4}}\\d{{4}}$)}", (string productId) => productId).WithDisplayName("RegexRoute");

        await app.StartAsync();

        var client = app.GetTestClient();

        _ = await client.GetAsync("https://localhost/products/abcd1234");
        Assert.Equal("RegexRoute", chosenRoute);
    }

    [Fact]
    public async Task UsingCreateSlimBuilderResultsInAlphaConstraintStillWorking()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        var chosenRoute = string.Empty;

        app.Use((context, next) =>
        {
            chosenRoute = context.GetEndpoint()?.DisplayName;
            return next(context);
        });

        app.MapGet("/products/{productId:alpha:minlength(4):maxlength(4)}", (string productId) => productId).WithDisplayName("AlphaRoute");

        await app.StartAsync();

        var client = app.GetTestClient();

        _ = await client.GetAsync("https://localhost/products/abcd");
        Assert.Equal("AlphaRoute", chosenRoute);
    }

    [Fact]
    public async Task UsingCreateSlimBuilderResultsInErrorWhenTryingToUseRegexConstraint()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.MapGet("/products/{productId:regex(^[a-z]{{4}}\\d{{4}}$)}", (string productId) => productId).WithDisplayName("AlphaRoute");

        await app.StartAsync();

        var client = app.GetTestClient();

        var ex = await Record.ExceptionAsync(async () =>
        {
            _ = await client.GetAsync("https://localhost/products/abcd1234");
        });

        Assert.IsType<RouteCreationException>(ex);
        Assert.IsType<InvalidOperationException>(ex.InnerException.InnerException);
        Assert.Equal(
            "A route parameter uses the regex constraint, which isn't registered. If this application was configured using CreateSlimBuilder(...) or AddRoutingCore(...) then this constraint is not registered by default. To use the regex constraint, configure route options at app startup: services.Configure<RouteOptions>(options => options.SetParameterPolicy<RegexInlineRouteConstraint>(\"regex\"));",
            ex.InnerException.InnerException.Message);
    }

    [Fact]
    public async Task UsingCreateSlimBuilderWorksIfRegexConstraintAddedViaAddRouting()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddRouting();
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        var chosenRoute = string.Empty;

        app.Use((context, next) =>
        {
            chosenRoute = context.GetEndpoint()?.DisplayName;
            return next(context);
        });

        app.MapGet("/products/{productId:regex(^[a-z]{{4}}\\d{{4}}$)}", (string productId) => productId).WithDisplayName("RegexRoute");

        await app.StartAsync();

        var client = app.GetTestClient();

        _ = await client.GetAsync("https://localhost/products/abcd1234");
        Assert.Equal("RegexRoute", chosenRoute);
    }

    [Fact]
    public async Task UsingCreateSlimBuilderWorksIfRegexConstraintAddedViaAddRoutingCoreWithActionDelegate()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddRoutingCore().Configure<RouteOptions>(options =>
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        var chosenRoute = string.Empty;

        app.Use((context, next) =>
        {
            chosenRoute = context.GetEndpoint()?.DisplayName;
            return next(context);
        });

        app.MapGet("/products/{productId:regex(^[a-z]{{4}}\\d{{4}}$)}", (string productId) => productId).WithDisplayName("RegexRoute");

        await app.StartAsync();

        var client = app.GetTestClient();

        _ = await client.GetAsync("https://localhost/products/abcd1234");
        Assert.Equal("RegexRoute", chosenRoute);
    }

    private sealed class TestDebugger : IDebugger
    {
        private bool _isAttached;
        public TestDebugger(bool isAttached) => _isAttached = isAttached;
        public bool IsAttached => _isAttached;
    }

    [Fact]
    public void DebugView_UseMiddleware_HasMiddleware()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IDebugger>(new TestDebugger(true));

        var app = builder.Build();

        app.UseMiddleware<MiddlewareWithInterface>();
        app.UseAuthentication();
        app.Use(next =>
        {
            return next;
        });

        var debugView = new WebApplication.WebApplicationDebugView(app);

        // Contains three strings:
        // 1. Middleware that implements IMiddleware from app.UseMiddleware<T>()
        // 2. AuthenticationMiddleware type from app.UseAuthentication()
        // 3. Generated delegate name from app.Use(...)
        Assert.Collection(debugView.Middleware,
            m => Assert.Equal(typeof(MiddlewareWithInterface).FullName, m),
            m => Assert.Equal("Microsoft.AspNetCore.Authentication.AuthenticationMiddleware", m),
            m =>
            {
                Assert.Contains(nameof(DebugView_UseMiddleware_HasMiddleware), m);
                Assert.DoesNotContain(nameof(RequestDelegate), m);
            });
    }

    [Fact]
    public void DebugView_NoDebugger_NoMiddleware()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IDebugger>(new TestDebugger(false));

        var app = builder.Build();

        app.UseMiddleware<MiddlewareWithInterface>();
        app.UseAuthentication();
        app.Use(next =>
        {
            return next;
        });

        var debugView = new WebApplication.WebApplicationDebugView(app);

        Assert.Throws<NotSupportedException>(() => debugView.Middleware);
    }

    [Fact]
    public async Task DebugView_UseMiddleware_HasEndpointsAndAuth_Run_HasAutomaticMiddleware()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthenticationCore();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IDebugger>(new TestDebugger(true));

        await using var app = builder.Build();

        app.UseMiddleware<MiddlewareWithInterface>();
        app.MapGet("/hello", () => "hello world");

        // Starting the app automatically adds middleware as needed.
        _ = app.RunAsync();

        var debugView = new WebApplication.WebApplicationDebugView(app);

        Assert.Collection(debugView.Middleware,
            m => Assert.Equal("Microsoft.AspNetCore.HostFiltering.HostFilteringMiddleware", m),
            m => Assert.Equal("Microsoft.AspNetCore.Routing.EndpointRoutingMiddleware", m),
            m => Assert.Equal("Microsoft.AspNetCore.Authentication.AuthenticationMiddleware", m),
            m => Assert.Equal("Microsoft.AspNetCore.Authorization.AuthorizationMiddlewareInternal", m),
            m => Assert.Equal(typeof(MiddlewareWithInterface).FullName, m),
            m => Assert.Equal("Microsoft.AspNetCore.Routing.EndpointMiddleware", m));
    }

    [Fact]
    public async Task DebugView_NoMiddleware_Run_HasAutomaticMiddleware()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IDebugger>(new TestDebugger(true));

        await using var app = builder.Build();

        // Starting the app automatically adds middleware as needed.
        _ = app.RunAsync();

        var debugView = new WebApplication.WebApplicationDebugView(app);

        Assert.Collection(debugView.Middleware,
            m => Assert.Equal("Microsoft.AspNetCore.HostFiltering.HostFilteringMiddleware", m));
    }

    [Fact]
    public void DebugView_NestedMiddleware_OnlyContainsTopLevelMiddleware()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IDebugger>(new TestDebugger(true));

        var app = builder.Build();

        app.MapWhen(c => true, nested =>
        {
            nested.UseStatusCodePages();
        });
        app.UseWhen(c => false, nested =>
        {
            nested.UseDeveloperExceptionPage();
        });
        app.UseExceptionHandler();

        var debugView = new WebApplication.WebApplicationDebugView(app);

        Assert.Equal(3, debugView.Middleware.Count);
    }

    [Fact]
    public async Task DebugView_Endpoints_AvailableBeforeAndAfterStart()
    {
        var builder = WebApplication.CreateBuilder();

        await using var app = builder.Build();
        app.MapGet("/hello", () => "hello world");

        var debugView = new WebApplication.WebApplicationDebugView(app);

        Assert.Collection(debugView.Endpoints,
            ep => Assert.Equal("/hello", ep.Metadata.GetRequiredMetadata<IRouteDiagnosticsMetadata>().Route));

        // Starting the app registers endpoint data sources with routing.
        _ = app.RunAsync();

        Assert.Collection(debugView.Endpoints,
            ep => Assert.Equal("/hello", ep.Metadata.GetRequiredMetadata<IRouteDiagnosticsMetadata>().Route));
    }

    [Fact]
    public async Task DebugView_Endpoints_UseEndpoints_AvailableBeforeAndAfterStart()
    {
        var builder = WebApplication.CreateBuilder();

        await using var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/hello", () => "hello world");
        });

        var debugView = new WebApplication.WebApplicationDebugView(app);

        Assert.Collection(debugView.Endpoints,
            ep => Assert.Equal("/hello", ep.Metadata.GetRequiredMetadata<IRouteDiagnosticsMetadata>().Route));

        // Starting the app registers endpoint data sources with routing.
        _ = app.RunAsync();

        Assert.Collection(debugView.Endpoints,
            ep => Assert.Equal("/hello", ep.Metadata.GetRequiredMetadata<IRouteDiagnosticsMetadata>().Route));
    }

    private class MiddlewareWithInterface : IMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            throw new NotImplementedException();
        }
    }

    private class UberHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public UberHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties) => Task.CompletedTask;

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties) => Task.CompletedTask;

        public Task<bool> HandleRequestAsync() => Task.FromResult(false);

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var username = Request.Query["username"];
            var principal = new ClaimsPrincipal();
            var id = new ClaimsIdentity();
            id.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, username));
            principal.AddIdentity(id);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, "custom")));
        }
    }

    public class RandomConfigurationSource : IConfigurationSource
    {
        public int ProvidersBuilt { get; set; }
        public int ProvidersLoaded { get; set; }
        public int ProvidersDisposed { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            ProvidersBuilt++;
            return new RandomConfigurationProvider(this);
        }
    }

    public class RandomConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly RandomConfigurationSource _source;

        public RandomConfigurationProvider(RandomConfigurationSource source)
        {
            _source = source;
        }

        public override void Load()
        {
            _source.ProvidersLoaded++;
            Data["Random"] = Guid.NewGuid().ToString();
        }

        public void Dispose() => _source.ProvidersDisposed++;
    }

    class ThrowingStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use((HttpContext context, RequestDelegate next) =>
                {
                    throw new InvalidOperationException("BOOM Filter");
                });

                next(app);
            };
        }
    }

    public class TestHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder
                .ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.AddInMemoryCollection(
                    new[]
                    {
                            new KeyValuePair<string,string>("testhostingstartup:config", "value")
                    }));
        }
    }

    class PropertyFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                // This should be true
                var val = app.Properties["didsomething"];
                Assert.True((bool)val);
            };
        }
    }

    private class Service : IService { }
    private interface IService { }

    private class Service2
    {
        public Service2(Service service)
        {
        }
    }

    private class MyStartup : IStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            throw new NotImplementedException();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class HostingListener : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly Action<IHostBuilder> _configure;
        private static readonly AsyncLocal<HostingListener> _currentListener = new();
        private readonly IDisposable _subscription0;
        private IDisposable _subscription1;

        public HostingListener(Action<IHostBuilder> configure)
        {
            _configure = configure;

            _subscription0 = DiagnosticListener.AllListeners.Subscribe(this);

            _currentListener.Value = this;
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(DiagnosticListener value)
        {
            if (_currentListener.Value != this)
            {
                // Ignore events that aren't for this listener
                return;
            }

            if (value.Name == "Microsoft.Extensions.Hosting")
            {
                _subscription1 = value.Subscribe(this);
            }
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (value.Key == "HostBuilding")
            {
                _configure?.Invoke((IHostBuilder)value.Value);
            }
        }

        public void Dispose()
        {
            // Undo this here just in case the code unwinds synchronously since that doesn't revert
            // the execution context to the original state. Only async methods do that on exit.
            _currentListener.Value = null;

            _subscription0.Dispose();
            _subscription1?.Dispose();
        }
    }

    private class CustomHostLifetime : IHostLifetime
    {
        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private class TestEventListener : EventListener
    {
        private volatile bool _disposed;

        private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

        public IEnumerable<EventWrittenEventArgs> EventData => _events;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Extensions-Logging")
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!_disposed)
            {
                _events.Enqueue(eventData);
            }
        }

        public override void Dispose()
        {
            _disposed = true;
            base.Dispose();
        }
    }

    private class ReloadableMemorySource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ReloadableMemoryProvider();
        }
    }

    private class ReloadableMemoryProvider : ConfigurationProvider
    {
        public override void Set(string key, string value)
        {
            base.Set(key, value);
            OnReload();
        }
    }

    private class MockAddressesServer : IServer
    {
        private readonly ICollection<string> _urls;

        public MockAddressesServer()
        {
            // For testing a server that doesn't set an IServerAddressesFeature.
        }

        public MockAddressesServer(ICollection<string> urls)
        {
            _urls = urls;

            var mockAddressesFeature = new MockServerAddressesFeature
            {
                Addresses = urls
            };

            Features.Set<IServerAddressesFeature>(mockAddressesFeature);
        }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
        {
            if (_urls.Count == 0)
            {
                // This is basically Kestrel's DefaultAddressStrategy.
                _urls.Add("http://localhost:5000");
                _urls.Add("https://localhost:5001");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        private class MockServerAddressesFeature : IServerAddressesFeature
        {
            public ICollection<string> Addresses { get; set; }
            public bool PreferHostingUrls { get; set; }
        }
    }

    private class TerminalMiddlewareStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                app.Run(context =>
                {
                    context.Response.StatusCode = 418; // I'm a teapot
                    return Task.CompletedTask;
                });
            };
        }
    }

    class UseRoutingStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseRouting();
                next(app);
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/1", async c =>
                    {
                        c.Response.StatusCode = 203;
                        await c.Response.WriteAsync("Hello Filter");
                    }).WithDisplayName("Two");
                });
            };
        }
    }

    private class MyServiceProviderFactory : IServiceProviderFactory<MyServiceProvider>
    {
        public MyServiceProvider CreateBuilder(IServiceCollection services) => new MyServiceProvider(services);

        public IServiceProvider CreateServiceProvider(MyServiceProvider containerBuilder)
        {
            containerBuilder.Build();
            return containerBuilder;
        }
    }

    private class MyServiceProvider : IServiceProvider
    {
        private IServiceProvider _inner;
        private IServiceCollection _services;

        public MyServiceProvider(IServiceCollection services) => _services = services;
        public void Build() => _inner = _services.BuildServiceProvider();
        public object GetService(Type serviceType) => _inner.GetService(serviceType);
    }
}
