// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using static Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for adding Azure diagnostics logger.
/// </summary>
public static class AzureAppServicesLoggerFactoryExtensions
{
    /// <summary>
    /// Adds an Azure Web Apps diagnostics logger.
    /// </summary>
    /// <param name="builder">The extension method argument</param>
    /// <returns></returns>
    public static ILoggingBuilder AddAzureWebAppDiagnostics(this ILoggingBuilder builder)
    {
        var context = WebAppContext.Default;

        // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
        return AddAzureWebAppDiagnostics(builder, context, _ => { });
    }

    /// <summary>
    /// Adds an Azure Web Apps diagnostics logger.
    /// </summary>
    /// <param name="builder">The extension method argument.</param>
    /// <param name="configureBlobLoggerOptions">An Action to configure the <see cref="AzureBlobLoggerOptions"/>.</param>
    /// <returns></returns>
    public static ILoggingBuilder AddAzureWebAppDiagnostics(this ILoggingBuilder builder, Action<AzureBlobLoggerOptions> configureBlobLoggerOptions)
    {
        var context = WebAppContext.Default;

        // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
        return AddAzureWebAppDiagnostics(builder, context, configureBlobLoggerOptions);
    }

    internal static ILoggingBuilder AddAzureWebAppDiagnostics(this ILoggingBuilder builder, IWebAppContext context, Action<AzureBlobLoggerOptions> configureBlobLoggerOptions)
    {
        if (!context.IsRunningInAzureWebApp)
        {
            return builder;
        }

        builder.AddConfiguration();

        var config = SiteConfigurationProvider.GetAzureLoggingConfiguration(context);
        var services = builder.Services;

        var addedFileLogger = TryAddEnumerable(services, Singleton<ILoggerProvider, FileLoggerProvider>());
        var addedBlobLogger = TryAddEnumerable(services, Singleton<ILoggerProvider, BlobLoggerProvider>());

        if (addedFileLogger || addedBlobLogger)
        {
            services.AddSingleton(context);
            services.AddSingleton<IOptionsChangeTokenSource<LoggerFilterOptions>>(
                new ConfigurationChangeTokenSource<LoggerFilterOptions>(config));
        }

        if (addedFileLogger)
        {
            services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(CreateFileFilterConfigureOptions(config));
            services.AddSingleton<IConfigureOptions<AzureFileLoggerOptions>>(new FileLoggerConfigureOptions(config, context));
            services.AddSingleton<IOptionsChangeTokenSource<AzureFileLoggerOptions>>(
                new ConfigurationChangeTokenSource<AzureFileLoggerOptions>(config));
            LoggerProviderOptions.RegisterProviderOptions<AzureFileLoggerOptions, FileLoggerProvider>(builder.Services);
        }

        if (addedBlobLogger)
        {
            services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(CreateBlobFilterConfigureOptions(config));
            services.AddSingleton<IConfigureOptions<AzureBlobLoggerOptions>>(new BlobLoggerConfigureOptions(config, context, configureBlobLoggerOptions));
            services.AddSingleton<IOptionsChangeTokenSource<AzureBlobLoggerOptions>>(
                new ConfigurationChangeTokenSource<AzureBlobLoggerOptions>(config));
            LoggerProviderOptions.RegisterProviderOptions<AzureBlobLoggerOptions, BlobLoggerProvider>(builder.Services);
        }

        return builder;
    }

    private static bool TryAddEnumerable(IServiceCollection collection, ServiceDescriptor descriptor)
    {
        var beforeCount = collection.Count;
        collection.TryAddEnumerable(descriptor);
        return beforeCount != collection.Count;
    }

    private static ConfigurationBasedLevelSwitcher CreateBlobFilterConfigureOptions(IConfiguration config)
    {
        return new ConfigurationBasedLevelSwitcher(
            configuration: config,
            provider: typeof(BlobLoggerProvider),
            levelKey: "AzureBlobTraceLevel");
    }

    private static ConfigurationBasedLevelSwitcher CreateFileFilterConfigureOptions(IConfiguration config)
    {
        return new ConfigurationBasedLevelSwitcher(
            configuration: config,
            provider: typeof(FileLoggerProvider),
            levelKey: "AzureDriveTraceLevel");
    }
}
