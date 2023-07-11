// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore;

/// <summary>
/// Provides convenience methods for creating instances of <see cref="IWebHost"/> and <see cref="IWebHostBuilder"/> with pre-configured defaults.
/// </summary>
public static class WebHost
{
    /// <summary>
    /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
    /// See <see cref="CreateDefaultBuilder()"/> for details.
    /// </summary>
    /// <param name="app">A delegate that handles requests to the application.</param>
    /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
    public static IWebHost Start(RequestDelegate app) =>
        Start(url: null!, app: app);

    /// <summary>
    /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
    /// See <see cref="CreateDefaultBuilder()"/> for details.
    /// </summary>
    /// <param name="url">The URL the hosted application will listen on.</param>
    /// <param name="app">A delegate that handles requests to the application.</param>
    /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
    public static IWebHost Start([StringSyntax(StringSyntaxAttribute.Uri)] string url, RequestDelegate app)
    {
        var startupAssemblyName = app.GetMethodInfo().DeclaringType!.Assembly.GetName().Name;
        return StartWith(url: url, configureServices: null, app: appBuilder => appBuilder.Run(app), applicationName: startupAssemblyName);
    }

    /// <summary>
    /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
    /// See <see cref="CreateDefaultBuilder()"/> for details.
    /// </summary>
    /// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
    /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
    public static IWebHost Start(Action<IRouteBuilder> routeBuilder) =>
        Start(url: null!, routeBuilder: routeBuilder);

    /// <summary>
    /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
    /// See <see cref="CreateDefaultBuilder()"/> for details.
    /// </summary>
    /// <param name="url">The URL the hosted application will listen on.</param>
    /// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
    /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
    public static IWebHost Start([StringSyntax(StringSyntaxAttribute.Uri)] string url, Action<IRouteBuilder> routeBuilder)
    {
        var startupAssemblyName = routeBuilder.GetMethodInfo().DeclaringType!.Assembly.GetName().Name;
        return StartWith(url, services => services.AddRouting(), appBuilder => appBuilder.UseRouter(routeBuilder), applicationName: startupAssemblyName);
    }

    /// <summary>
    /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
    /// See <see cref="CreateDefaultBuilder()"/> for details.
    /// </summary>
    /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
    public static IWebHost StartWith(Action<IApplicationBuilder> app) =>
        StartWith(url: null!, app: app);

    /// <summary>
    /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
    /// See <see cref="CreateDefaultBuilder()"/> for details.
    /// </summary>
    /// <param name="url">The URL the hosted application will listen on.</param>
    /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
    public static IWebHost StartWith([StringSyntax(StringSyntaxAttribute.Uri)] string url, Action<IApplicationBuilder> app) =>
        StartWith(url: url, configureServices: null, app: app, applicationName: null);

    private static IWebHost StartWith(string? url, Action<IServiceCollection>? configureServices, Action<IApplicationBuilder> app, string? applicationName)
    {
        var builder = CreateDefaultBuilder();

        if (!string.IsNullOrEmpty(url))
        {
            builder.UseUrls(url);
        }

        if (configureServices != null)
        {
            builder.ConfigureServices(configureServices);
        }

        builder.Configure(app);

        if (!string.IsNullOrEmpty(applicationName))
        {
            builder.UseSetting(WebHostDefaults.ApplicationKey, applicationName);
        }

        var host = builder.Build();

        host.Start();

        return host;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
    /// </summary>
    /// <remarks>
    ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
    ///     use Kestrel as the web server and configure it using the application's configuration providers,
    ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
    ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
    ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
    ///     load <see cref="IConfiguration"/> from environment variables,
    ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
    ///     adds the HostFiltering middleware,
    ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
    ///     and enable IIS integration.
    /// </remarks>
    /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder CreateDefaultBuilder() =>
        CreateDefaultBuilder(args: null!);

    /// <summary>
    /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
    /// </summary>
    /// <remarks>
    ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
    ///     use Kestrel as the web server and configure it using the application's configuration providers,
    ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
    ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
    ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
    ///     load <see cref="IConfiguration"/> from environment variables,
    ///     load <see cref="IConfiguration"/> from supplied command line args,
    ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
    ///     configure the <see cref="IWebHostEnvironment.WebRootFileProvider"/> to map static web assets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
    ///     adds the HostFiltering middleware,
    ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
    ///     and enable IIS integration.
    /// </remarks>
    /// <param name="args">The command line args.</param>
    /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder CreateDefaultBuilder(string[] args)
    {
        var builder = new WebHostBuilder();

        if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ContentRootKey)))
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory());
        }
        if (args != null)
        {
            builder.UseConfiguration(new ConfigurationBuilder().AddCommandLine(args).Build());
        }

        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            var env = hostingContext.HostingEnvironment;

            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

            if (env.IsDevelopment())
            {
                if (!string.IsNullOrEmpty(env.ApplicationName))
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }
            }

            config.AddEnvironmentVariables();

            if (args != null)
            {
                config.AddCommandLine(args);
            }
        })
        .ConfigureLogging((hostingContext, loggingBuilder) =>
        {
            loggingBuilder.Configure(options =>
            {
                options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                                    | ActivityTrackingOptions.TraceId
                                                    | ActivityTrackingOptions.ParentId;
            });
            loggingBuilder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
            loggingBuilder.AddDebug();
            loggingBuilder.AddEventSourceLogger();
        }).
        UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
        });

        ConfigureWebDefaults(builder);

        return builder;
    }

    internal static void ConfigureWebDefaults(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, cb) =>
        {
            if (ctx.HostingEnvironment.IsDevelopment())
            {
                StaticWebAssetsLoader.UseStaticWebAssets(ctx.HostingEnvironment, ctx.Configuration);
            }
        });

        ConfigureWebDefaultsWorker(
            builder.UseKestrel(ConfigureKestrel),
            services =>
            {
                services.AddRouting();
            });

        builder
            .UseIIS()
            .UseIISIntegration();
    }

    internal static void ConfigureWebDefaultsSlim(IWebHostBuilder builder)
    {
        ConfigureWebDefaultsWorker(builder.UseKestrelCore().ConfigureKestrel(ConfigureKestrel), configureRouting: null);
    }

    private static void ConfigureKestrel(WebHostBuilderContext builderContext, KestrelServerOptions options)
    {
        options.Configure(builderContext.Configuration.GetSection("Kestrel"), reloadOnChange: true);
    }

    private static void ConfigureWebDefaultsWorker(IWebHostBuilder builder, Action<IServiceCollection>? configureRouting)
    {
        builder.ConfigureServices((hostingContext, services) =>
        {
            // Fallback
            services.PostConfigure<HostFilteringOptions>(options =>
            {
                if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
                {
                    // "AllowedHosts": "localhost;127.0.0.1;[::1]"
                    var hosts = hostingContext.Configuration["AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    // Fall back to "*" to disable.
                    options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
                }
            });
            // Change notification
            services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
                    new ConfigurationChangeTokenSource<HostFilteringOptions>(hostingContext.Configuration));

            services.AddTransient<IStartupFilter, HostFilteringStartupFilter>();
            services.AddTransient<IStartupFilter, ForwardedHeadersStartupFilter>();
            services.AddTransient<IConfigureOptions<ForwardedHeadersOptions>, ForwardedHeadersOptionsSetup>();

            // Provide a way for the default host builder to configure routing. This probably means calling AddRouting.
            // A lambda is used here because we don't want to reference AddRouting directly because of trimming.
            // This avoids the overhead of calling AddRoutingCore multiple times on app startup.
            if (configureRouting == null)
            {
                services.AddRoutingCore();
            }
            else
            {
                configureRouting(services);
            }
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
    /// </summary>
    /// <remarks>
    ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
    ///     use Kestrel as the web server and configure it using the application's configuration providers,
    ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
    ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
    ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
    ///     load <see cref="IConfiguration"/> from environment variables,
    ///     load <see cref="IConfiguration"/> from supplied command line args,
    ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
    ///     enable IIS integration.
    /// </remarks>
    /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
    /// <param name="args">The command line args.</param>
    /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder CreateDefaultBuilder<[DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)] TStartup>(string[] args) where TStartup : class =>
        CreateDefaultBuilder(args).UseStartup<TStartup>();
}
