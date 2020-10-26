// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Hosting
{
    internal class GenericWebHostBuilder : IWebHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
    {
        private readonly IHostBuilder _builder;
        private readonly IConfiguration _config;
        private readonly object _startupKey = new object();

        private AggregateException _hostingStartupErrors;
        private HostingStartupWebHostBuilder _hostingStartupWebHostBuilder;

        public GenericWebHostBuilder(IHostBuilder builder)
        {
            _builder = builder;

            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            _builder.ConfigureHostConfiguration(config =>
            {
                config.AddConfiguration(_config);

                // We do this super early but still late enough that we can process the configuration
                // wired up by calls to UseSetting
                ExecuteHostingStartups();
            });

            // IHostingStartup needs to be executed before any direct methods on the builder
            // so register these callbacks first
            _builder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                if (_hostingStartupWebHostBuilder != null)
                {
                    var webhostContext = GetWebHostBuilderContext(context);
                    _hostingStartupWebHostBuilder.ConfigureAppConfiguration(webhostContext, configurationBuilder);
                }
            });

            _builder.ConfigureServices((context, services) =>
            {
                var webhostContext = GetWebHostBuilderContext(context);
                var webHostOptions = (WebHostOptions)context.Properties[typeof(WebHostOptions)];

                // Add the IHostingEnvironment and IApplicationLifetime from Microsoft.AspNetCore.Hosting
                services.AddSingleton(webhostContext.HostingEnvironment);
#pragma warning disable CS0618 // Type or member is obsolete
                services.AddSingleton((AspNetCore.Hosting.IHostingEnvironment)webhostContext.HostingEnvironment);
                services.AddSingleton<IApplicationLifetime, GenericWebHostApplicationLifetime>();
#pragma warning restore CS0618 // Type or member is obsolete

                services.Configure<GenericWebHostServiceOptions>(options =>
                {
                    // Set the options
                    options.WebHostOptions = webHostOptions;
                    // Store and forward any startup errors
                    options.HostingStartupExceptions = _hostingStartupErrors;
                });

                // REVIEW: This is bad since we don't own this type. Anybody could add one of these and it would mess things up
                // We need to flow this differently
                var listener = new DiagnosticListener("Microsoft.AspNetCore");
                services.TryAddSingleton<DiagnosticListener>(listener);
                services.TryAddSingleton<DiagnosticSource>(listener);

                services.TryAddSingleton<IHttpContextFactory, DefaultHttpContextFactory>();
                services.TryAddScoped<IMiddlewareFactory, MiddlewareFactory>();
                services.TryAddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();

                // IMPORTANT: This needs to run *before* direct calls on the builder (like UseStartup)
                _hostingStartupWebHostBuilder?.ConfigureServices(webhostContext, services);

                // Support UseStartup(assemblyName)
                if (!string.IsNullOrEmpty(webHostOptions.StartupAssembly))
                {
                    try
                    {
                        var startupType = StartupLoader.FindStartupType(webHostOptions.StartupAssembly, webhostContext.HostingEnvironment.EnvironmentName);
                        UseStartup(startupType, context, services);
                    }
                    catch (Exception ex) when (webHostOptions.CaptureStartupErrors)
                    {
                        var capture = ExceptionDispatchInfo.Capture(ex);

                        services.Configure<GenericWebHostServiceOptions>(options =>
                        {
                            options.ConfigureApplication = app =>
                            {
                                // Throw if there was any errors initializing startup
                                capture.Throw();
                            };
                        });
                    }
                }
            });
        }

        private void ExecuteHostingStartups()
        {
            var webHostOptions = new WebHostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name);

            if (webHostOptions.PreventHostingStartup)
            {
                return;
            }

            var exceptions = new List<Exception>();
            _hostingStartupWebHostBuilder = new HostingStartupWebHostBuilder(this);

            // Execute the hosting startup assemblies
            foreach (var assemblyName in webHostOptions.GetFinalHostingStartupAssemblies().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var assembly = Assembly.Load(new AssemblyName(assemblyName));

                    foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                    {
                        var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType);
                        hostingStartup.Configure(_hostingStartupWebHostBuilder);
                    }
                }
                catch (Exception ex)
                {
                    // Capture any errors that happen during startup
                    exceptions.Add(new InvalidOperationException($"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.", ex));
                }
            }

            if (exceptions.Count > 0)
            {
                _hostingStartupErrors = new AggregateException(exceptions);
            }
        }

        public IWebHost Build()
        {
            throw new NotSupportedException($"Building this implementation of {nameof(IWebHostBuilder)} is not supported.");
        }

        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureAppConfiguration((context, builder) =>
            {
                var webhostBuilderContext = GetWebHostBuilderContext(context);
                configureDelegate(webhostBuilderContext, builder);
            });

            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return ConfigureServices((context, services) => configureServices(services));
        }

        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            _builder.ConfigureServices((context, builder) =>
            {
                var webhostBuilderContext = GetWebHostBuilderContext(context);
                configureServices(webhostBuilderContext, builder);
            });

            return this;
        }

        public IWebHostBuilder UseDefaultServiceProvider(Action<WebHostBuilderContext, ServiceProviderOptions> configure)
        {
            _builder.UseServiceProviderFactory(context =>
            {
                var webHostBuilderContext = GetWebHostBuilderContext(context);
                var options = new ServiceProviderOptions();
                configure(webHostBuilderContext, options);
                return new DefaultServiceProviderFactory(options);
            });

            return this;
        }

        public IWebHostBuilder UseStartup(Type startupType)
        {
            // UseStartup can be called multiple times. Only run the last one.
            _builder.Properties["UseStartup.StartupType"] = startupType;
            _builder.ConfigureServices((context, services) =>
            {
                if (_builder.Properties.TryGetValue("UseStartup.StartupType", out var cachedType) && (Type)cachedType == startupType)
                {
                    UseStartup(startupType, context, services);
                }
            });

            return this;
        }

        private void UseStartup(Type startupType, HostBuilderContext context, IServiceCollection services)
        {
            var webHostBuilderContext = GetWebHostBuilderContext(context);
            var webHostOptions = (WebHostOptions)context.Properties[typeof(WebHostOptions)];

            ExceptionDispatchInfo startupError = null;
            object instance = null;
            ConfigureBuilder configureBuilder = null;

            try
            {
                // We cannot support methods that return IServiceProvider as that is terminal and we need ConfigureServices to compose
                if (typeof(IStartup).IsAssignableFrom(startupType))
                {
                    throw new NotSupportedException($"{typeof(IStartup)} isn't supported");
                }
                if (StartupLoader.HasConfigureServicesIServiceProviderDelegate(startupType, context.HostingEnvironment.EnvironmentName))
                {
                    throw new NotSupportedException($"ConfigureServices returning an {typeof(IServiceProvider)} isn't supported.");
                }

                instance = ActivatorUtilities.CreateInstance(new HostServiceProvider(webHostBuilderContext), startupType);
                context.Properties[_startupKey] = instance;

                // Startup.ConfigureServices
                var configureServicesBuilder = StartupLoader.FindConfigureServicesDelegate(startupType, context.HostingEnvironment.EnvironmentName);
                var configureServices = configureServicesBuilder.Build(instance);

                configureServices(services);

                // REVIEW: We're doing this in the callback so that we have access to the hosting environment
                // Startup.ConfigureContainer
                var configureContainerBuilder = StartupLoader.FindConfigureContainerDelegate(startupType, context.HostingEnvironment.EnvironmentName);
                if (configureContainerBuilder.MethodInfo != null)
                {
                    var containerType = configureContainerBuilder.GetContainerType();
                    // Store the builder in the property bag
                    _builder.Properties[typeof(ConfigureContainerBuilder)] = configureContainerBuilder;

                    var actionType = typeof(Action<,>).MakeGenericType(typeof(HostBuilderContext), containerType);

                    // Get the private ConfigureContainer method on this type then close over the container type
                    var configureCallback = GetType().GetMethod(nameof(ConfigureContainer), BindingFlags.NonPublic | BindingFlags.Instance)
                                                     .MakeGenericMethod(containerType)
                                                     .CreateDelegate(actionType, this);

                    // _builder.ConfigureContainer<T>(ConfigureContainer);
                    typeof(IHostBuilder).GetMethods().First(m => m.Name == nameof(IHostBuilder.ConfigureContainer))
                        .MakeGenericMethod(containerType)
                        .InvokeWithoutWrappingExceptions(_builder, new object[] { configureCallback });
                }

                // Resolve Configure after calling ConfigureServices and ConfigureContainer
                configureBuilder = StartupLoader.FindConfigureDelegate(startupType, context.HostingEnvironment.EnvironmentName);
            }
            catch (Exception ex) when (webHostOptions.CaptureStartupErrors)
            {
                startupError = ExceptionDispatchInfo.Capture(ex);
            }

            // Startup.Configure
            services.Configure<GenericWebHostServiceOptions>(options =>
            {
                options.ConfigureApplication = app =>
                {
                    // Throw if there was any errors initializing startup
                    startupError?.Throw();

                    // Execute Startup.Configure
                    if (instance != null && configureBuilder != null)
                    {
                        configureBuilder.Build(instance)(app);
                    }
                };
            });
        }

        private void ConfigureContainer<TContainer>(HostBuilderContext context, TContainer container)
        {
            var instance = context.Properties[_startupKey];
            var builder = (ConfigureContainerBuilder)context.Properties[typeof(ConfigureContainerBuilder)];
            builder.Build(instance)(container);
        }

        public IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure)
        {
            _builder.ConfigureServices((context, services) =>
            {
                services.Configure<GenericWebHostServiceOptions>(options =>
                {
                    var webhostBuilderContext = GetWebHostBuilderContext(context);
                    options.ConfigureApplication = app => configure(webhostBuilderContext, app);
                });
            });

            return this;
        }

        private WebHostBuilderContext GetWebHostBuilderContext(HostBuilderContext context)
        {
            if (!context.Properties.TryGetValue(typeof(WebHostBuilderContext), out var contextVal))
            {
                var options = new WebHostOptions(context.Configuration, Assembly.GetEntryAssembly()?.GetName().Name);
                var webHostBuilderContext = new WebHostBuilderContext
                {
                    Configuration = context.Configuration,
                    HostingEnvironment = new HostingEnvironment(),
                };
                webHostBuilderContext.HostingEnvironment.Initialize(context.HostingEnvironment.ContentRootPath, options);
                context.Properties[typeof(WebHostBuilderContext)] = webHostBuilderContext;
                context.Properties[typeof(WebHostOptions)] = options;
                return webHostBuilderContext;
            }

            // Refresh config, it's periodically updated/replaced
            var webHostContext = (WebHostBuilderContext)contextVal;
            webHostContext.Configuration = context.Configuration;
            return webHostContext;
        }

        public string GetSetting(string key)
        {
            return _config[key];
        }

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        // This exists just so that we can use ActivatorUtilities.CreateInstance on the Startup class
        private class HostServiceProvider : IServiceProvider
        {
            private readonly WebHostBuilderContext _context;

            public HostServiceProvider(WebHostBuilderContext context)
            {
                _context = context;
            }

            public object GetService(Type serviceType)
            {
                // The implementation of the HostingEnvironment supports both interfaces
#pragma warning disable CS0618 // Type or member is obsolete
                if (serviceType == typeof(Microsoft.Extensions.Hosting.IHostingEnvironment)
                    || serviceType == typeof(Microsoft.AspNetCore.Hosting.IHostingEnvironment)
#pragma warning restore CS0618 // Type or member is obsolete
                    || serviceType == typeof(IWebHostEnvironment)
                    || serviceType == typeof(IHostEnvironment)
                    )
                {
                    return _context.HostingEnvironment;
                }

                if (serviceType == typeof(IConfiguration))
                {
                    return _context.Configuration;
                }

                return null;
            }
        }
    }
}
