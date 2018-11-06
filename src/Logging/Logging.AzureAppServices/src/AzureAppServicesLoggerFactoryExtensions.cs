// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.Extensions.Logging.AzureAppServices.Internal;
using Microsoft.Extensions.Options;
using static Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for adding Azure diagnostics logger.
    /// </summary>
    public static class AzureAppServicesLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an Azure Web Apps diagnostics logger.
        /// </summary>
        /// <param name="builder">The extension method argument</param>
        public static ILoggingBuilder AddAzureWebAppDiagnostics(this ILoggingBuilder builder)
        {
            var context = WebAppContext.Default;

            // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
            return AddAzureWebAppDiagnostics(builder, context);
        }

        internal static ILoggingBuilder AddAzureWebAppDiagnostics(this ILoggingBuilder builder, IWebAppContext context)
        {
            if (!context.IsRunningInAzureWebApp)
            {
                return builder;
            }

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
            }

            if (addedBlobLogger)
            {
                services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(CreateBlobFilterConfigureOptions(config));
                services.AddSingleton<IConfigureOptions<AzureBlobLoggerOptions>>(new BlobLoggerConfigureOptions(config, context));
                services.AddSingleton<IOptionsChangeTokenSource<AzureBlobLoggerOptions>>(
                    new ConfigurationChangeTokenSource<AzureBlobLoggerOptions>(config));
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

        /// <summary>
        /// Adds an Azure Web Apps diagnostics logger.
        /// </summary>
        /// <param name="factory">The extension method argument</param>
        public static ILoggerFactory AddAzureWebAppDiagnostics(this ILoggerFactory factory)
        {
            return AddAzureWebAppDiagnostics(factory, new AzureAppServicesDiagnosticsSettings());
        }

        /// <summary>
        /// Adds an Azure Web Apps diagnostics logger.
        /// </summary>
        /// <param name="factory">The extension method argument</param>
        /// <param name="settings">The setting object to configure loggers.</param>
        public static ILoggerFactory AddAzureWebAppDiagnostics(this ILoggerFactory factory, AzureAppServicesDiagnosticsSettings settings)
        {
            var context = WebAppContext.Default;
            if (!context.IsRunningInAzureWebApp)
            {
                return factory;
            }

            var config = SiteConfigurationProvider.GetAzureLoggingConfiguration(context);

            // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
            var fileOptions = new OptionsMonitor<AzureFileLoggerOptions>(
                new OptionsFactory<AzureFileLoggerOptions>(
                    new IConfigureOptions<AzureFileLoggerOptions>[]
                    {
                        new FileLoggerConfigureOptions(config, context),
                        new ConfigureOptions<AzureFileLoggerOptions>(options =>
                        {
                            options.FileSizeLimit = settings.FileSizeLimit;
                            options.RetainedFileCountLimit = settings.RetainedFileCountLimit;
                            options.BackgroundQueueSize = settings.BackgroundQueueSize == 0 ? (int?) null : settings.BackgroundQueueSize;

                            if (settings.FileFlushPeriod != null)
                            {
                                options.FlushPeriod = settings.FileFlushPeriod.Value;
                            }
                        })
                    },
                    new IPostConfigureOptions<AzureFileLoggerOptions>[0]
                ),
                new[]
                {
                    new ConfigurationChangeTokenSource<AzureFileLoggerOptions>(config)
                },
                new OptionsCache<AzureFileLoggerOptions>()
            );

            var blobOptions = new OptionsMonitor<AzureBlobLoggerOptions>(
                new OptionsFactory<AzureBlobLoggerOptions>(
                    new IConfigureOptions<AzureBlobLoggerOptions>[] {
                        new BlobLoggerConfigureOptions(config, context),
                        new ConfigureOptions<AzureBlobLoggerOptions>(options =>
                        {
                            options.BlobName = settings.BlobName;
                            options.FlushPeriod = settings.BlobCommitPeriod;
                            options.BatchSize = settings.BlobBatchSize;
                            options.BackgroundQueueSize = settings.BackgroundQueueSize == 0 ? (int?) null : settings.BackgroundQueueSize;
                        })
                    },
                    new IPostConfigureOptions<AzureBlobLoggerOptions>[0]
                ),
                new[]
                {
                    new ConfigurationChangeTokenSource<AzureBlobLoggerOptions>(config)
                },
                new OptionsCache<AzureBlobLoggerOptions>()
            );

            var filterOptions = new OptionsMonitor<LoggerFilterOptions>(
                new OptionsFactory<LoggerFilterOptions>(
                    new[]
                    {
                        CreateFileFilterConfigureOptions(config),
                        CreateBlobFilterConfigureOptions(config)
                    },
                    new IPostConfigureOptions<LoggerFilterOptions>[0]),
                new [] { new ConfigurationChangeTokenSource<LoggerFilterOptions>(config) },
                new OptionsCache<LoggerFilterOptions>());

            factory.AddProvider(new ForwardingLoggerProvider(
                new LoggerFactory(
                    new ILoggerProvider[]
                    {
                        new FileLoggerProvider(fileOptions),
                        new BlobLoggerProvider(blobOptions)
                    },
                    filterOptions
                )
            ));
            return factory;
        }
    }
}
