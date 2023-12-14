// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// Contains extensions for configuring the <see cref="IWebHostBuilder" /> instance.
/// </summary>
public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Enables the <see cref="TestServer" /> service.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseTestServer(this IWebHostBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            services.AddSingleton<IHostLifetime, NoopHostLifetime>();
            services.AddSingleton<IServer, TestServer>();
        });
    }

    /// <summary>
    /// Enables the <see cref="TestServer" /> service.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    /// <param name="configureOptions">Configures test server options</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseTestServer(this IWebHostBuilder builder, Action<TestServerOptions> configureOptions)
    {
        return builder.ConfigureServices(services =>
        {
            services.Configure(configureOptions);
            services.AddSingleton<IHostLifetime, NoopHostLifetime>();
            services.AddSingleton<IServer, TestServer>();
        });
    }

    /// <summary>
    /// Retrieves the TestServer from the host services.
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static TestServer GetTestServer(this IWebHost host)
    {
        return (TestServer)host.Services.GetRequiredService<IServer>();
    }

    /// <summary>
    /// Retrieves the test client from the TestServer in the host services.
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static HttpClient GetTestClient(this IWebHost host)
    {
        return host.GetTestServer().CreateClient();
    }

    /// <summary>
    /// Configures the <see cref="IWebHostBuilder" /> instance with the services provided in <paramref name="servicesConfiguration" />.
    /// </summary>
    /// <param name="webHostBuilder">The <see cref="IWebHostBuilder"/>.</param>
    /// <param name="servicesConfiguration">An <see cref="Action"/> that registers services onto the <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder ConfigureTestServices(this IWebHostBuilder webHostBuilder, Action<IServiceCollection> servicesConfiguration)
    {
        ArgumentNullException.ThrowIfNull(webHostBuilder);
        ArgumentNullException.ThrowIfNull(servicesConfiguration);

        if (webHostBuilder.GetType().Name.Equals("GenericWebHostBuilder", StringComparison.Ordinal))
        {
            // Generic host doesn't need to do anything special here since there's only one container.
            webHostBuilder.ConfigureServices(servicesConfiguration);
        }
        else
        {
#pragma warning disable CS0612 // Type or member is obsolete
            webHostBuilder.ConfigureServices(
                s => s.AddSingleton<IStartupConfigureServicesFilter>(
                    new ConfigureTestServicesStartupConfigureServicesFilter(servicesConfiguration)));
#pragma warning restore CS0612 // Type or member is obsolete
        }

        return webHostBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IWebHostBuilder" /> instance with the services provided in <paramref name="servicesConfiguration" />.
    /// </summary>
    /// <param name="webHostBuilder">The <see cref="IWebHostBuilder"/>.</param>
    /// <param name="servicesConfiguration">An <see cref="Action"/> that registers services onto the <typeparamref name="TContainer"/>.</param>
    /// <typeparam name="TContainer">A collection of service descriptors.</typeparam>
    /// <returns></returns>
    public static IWebHostBuilder ConfigureTestContainer<TContainer>(this IWebHostBuilder webHostBuilder, Action<TContainer> servicesConfiguration)
    {
        ArgumentNullException.ThrowIfNull(webHostBuilder);
        ArgumentNullException.ThrowIfNull(servicesConfiguration);

#pragma warning disable CS0612 // Type or member is obsolete
        webHostBuilder.ConfigureServices(
            s => s.AddSingleton<IStartupConfigureContainerFilter<TContainer>>(
                new ConfigureTestServicesStartupConfigureContainerFilter<TContainer>(servicesConfiguration)));
#pragma warning restore CS0612 // Type or member is obsolete

        return webHostBuilder;
    }

    /// <summary>
    /// Sets the content root of relative to the <paramref name="solutionRelativePath" />.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    /// <param name="solutionRelativePath">The directory of the solution file.</param>
    /// <param name="solutionName">The name of the solution file to make the content root relative to.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static IWebHostBuilder UseSolutionRelativeContentRoot(
        this IWebHostBuilder builder,
        string solutionRelativePath,
        string solutionName = "*.sln")
    {
        return builder.UseSolutionRelativeContentRoot(solutionRelativePath, AppContext.BaseDirectory, solutionName);
    }

    /// <summary>
    /// Sets the content root of relative to the <paramref name="solutionRelativePath" />.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    /// <param name="solutionRelativePath">The directory of the solution file.</param>
    /// <param name="applicationBasePath">The root of the app's directory.</param>
    /// <param name="solutionName">The name of the solution file to make the content root relative to.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static IWebHostBuilder UseSolutionRelativeContentRoot(
        this IWebHostBuilder builder,
        string solutionRelativePath,
        string applicationBasePath,
        string solutionName = "*.sln")
    {
        ArgumentNullException.ThrowIfNull(solutionRelativePath);
        ArgumentNullException.ThrowIfNull(applicationBasePath);

        var directoryInfo = new DirectoryInfo(applicationBasePath);
        do
        {
            var solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, solutionName).FirstOrDefault();
            if (solutionPath != null)
            {
                builder.UseContentRoot(Path.GetFullPath(Path.Combine(directoryInfo.FullName, solutionRelativePath)));
                return builder;
            }

            directoryInfo = directoryInfo.Parent;
        }
        while (directoryInfo is not null);

        throw new InvalidOperationException($"Solution root could not be located using application root {applicationBasePath}.");
    }

#pragma warning disable CS0612 // Type or member is obsolete
    private sealed class ConfigureTestServicesStartupConfigureServicesFilter : IStartupConfigureServicesFilter
#pragma warning restore CS0612 // Type or member is obsolete
    {
        private readonly Action<IServiceCollection> _servicesConfiguration;

        public ConfigureTestServicesStartupConfigureServicesFilter(Action<IServiceCollection> servicesConfiguration)
        {
            ArgumentNullException.ThrowIfNull(servicesConfiguration);

            _servicesConfiguration = servicesConfiguration;
        }

        public Action<IServiceCollection> ConfigureServices(Action<IServiceCollection> next) =>
            serviceCollection =>
            {
                next(serviceCollection);
                _servicesConfiguration(serviceCollection);
            };
    }

#pragma warning disable CS0612 // Type or member is obsolete
    private sealed class ConfigureTestServicesStartupConfigureContainerFilter<TContainer> : IStartupConfigureContainerFilter<TContainer>
#pragma warning restore CS0612 // Type or member is obsolete
    {
        private readonly Action<TContainer> _servicesConfiguration;

        public ConfigureTestServicesStartupConfigureContainerFilter(Action<TContainer> containerConfiguration)
        {
            ArgumentNullException.ThrowIfNull(containerConfiguration);

            _servicesConfiguration = containerConfiguration;
        }

        public Action<TContainer> ConfigureContainer(Action<TContainer> next) =>
            containerBuilder =>
            {
                next(containerBuilder);
                _servicesConfiguration(containerBuilder);
            };
    }
}
