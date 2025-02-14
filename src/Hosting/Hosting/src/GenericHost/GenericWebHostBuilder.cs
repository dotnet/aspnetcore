// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Infrastructure;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class GenericWebHostBuilder : WebHostBuilderBase, ISupportsStartup
{
    private object? _startupObject;
    private readonly object _startupKey = new object();

    private AggregateException? _hostingStartupErrors;
    private HostingStartupWebHostBuilder? _hostingStartupWebHostBuilder;

    public GenericWebHostBuilder(IHostBuilder builder, WebHostBuilderOptions options)
        : base(builder, options)
    {
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
            services.TryAddSingleton(sp => new DiagnosticListener("Microsoft.AspNetCore"));
            services.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());
            services.TryAddSingleton(sp => new ActivitySource("Microsoft.AspNetCore"));
            services.TryAddSingleton(DistributedContextPropagator.Current);

            services.TryAddSingleton<IHttpContextFactory, DefaultHttpContextFactory>();
            services.TryAddScoped<IMiddlewareFactory, MiddlewareFactory>();
            services.TryAddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();

            services.AddMetrics();
            services.TryAddSingleton<HostingMetrics>();

            // IMPORTANT: This needs to run *before* direct calls on the builder (like UseStartup)
            _hostingStartupWebHostBuilder?.ConfigureServices(webhostContext, services);

            // Support UseStartup(assemblyName)
            if (!string.IsNullOrEmpty(webHostOptions.StartupAssembly))
            {
                ScanAssemblyAndRegisterStartup(context, services, webhostContext, webHostOptions);
            }
        });
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2072", Justification = "Finding startup type in assembly requires unreferenced code. Surfaced to user in UseStartup(assemblyName).")]
    private void ScanAssemblyAndRegisterStartup(HostBuilderContext context, IServiceCollection services, WebHostBuilderContext webhostContext, WebHostOptions webHostOptions)
    {
        try
        {
            var startupType = StartupLoader.FindStartupType(webHostOptions.StartupAssembly!, webhostContext.HostingEnvironment.EnvironmentName);
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

    private void ExecuteHostingStartups()
    {
        var webHostOptions = new WebHostOptions(_config);

        if (webHostOptions.PreventHostingStartup)
        {
            return;
        }

        var exceptions = new List<Exception>();
        var processed = new HashSet<Assembly>();

        _hostingStartupWebHostBuilder = new HostingStartupWebHostBuilder(this);

        // Execute the hosting startup assemblies
        foreach (var assemblyName in webHostOptions.GetFinalHostingStartupAssemblies())
        {
            try
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));

                if (!processed.Add(assembly))
                {
                    // Already processed, skip it
                    continue;
                }

                foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                {
                    var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType)!;
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

    public IWebHostBuilder UseStartup([DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)] Type startupType)
    {
        var startupAssemblyName = startupType.Assembly.GetName().Name;

        UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName);

        // UseStartup can be called multiple times. Only run the last one.
        _startupObject = startupType;

        _builder.ConfigureServices((context, services) =>
        {
            // Run this delegate if the startup type matches
            if (object.ReferenceEquals(_startupObject, startupType))
            {
                UseStartup(startupType, context, services);
            }
        });

        return this;
    }

    // Note: This method isn't 100% compatible with trimming. It is possible for the factory to return a derived type from TStartup.
    // RequiresUnreferencedCode isn't on this method because the majority of people won't do that.
    public IWebHostBuilder UseStartup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TStartup>(Func<WebHostBuilderContext, TStartup> startupFactory)
    {
        var startupAssemblyName = startupFactory.GetMethodInfo().DeclaringType!.Assembly.GetName().Name;

        UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName);

        // Clear the startup type
        _startupObject = startupFactory;

        _builder.ConfigureServices(ConfigureStartup);

        [UnconditionalSuppressMessage("Trimmer", "IL2072", Justification = "Startup type created by factory can't be determined statically.")]
        void ConfigureStartup(HostBuilderContext context, IServiceCollection services)
        {
            // UseStartup can be called multiple times. Only run the last one.
            if (object.ReferenceEquals(_startupObject, startupFactory))
            {
                var webHostBuilderContext = GetWebHostBuilderContext(context);
                var instance = startupFactory(webHostBuilderContext) ?? throw new InvalidOperationException("The specified factory returned null startup instance.");
                UseStartup(instance.GetType(), context, services, instance);
            }
        }

        return this;
    }

    private void UseStartup([DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)] Type startupType, HostBuilderContext context, IServiceCollection services, object? instance = null)
    {
        var webHostBuilderContext = GetWebHostBuilderContext(context);
        var webHostOptions = (WebHostOptions)context.Properties[typeof(WebHostOptions)];

        ExceptionDispatchInfo? startupError = null;
        ConfigureBuilder? configureBuilder = null;

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

            instance ??= ActivatorUtilities.CreateInstance(new HostServiceProvider(webHostBuilderContext), startupType);
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
                // Store the builder in the property bag
                _builder.Properties[typeof(ConfigureContainerBuilder)] = configureContainerBuilder;

                InvokeContainer(this, configureContainerBuilder);
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

        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
            Justification = "There is a runtime check for ValueType startup container. It's unlikely anyone will use a ValueType here.")]
        static void InvokeContainer(GenericWebHostBuilder genericWebHostBuilder, ConfigureContainerBuilder configureContainerBuilder)
        {
            var containerType = configureContainerBuilder.GetContainerType();

            // Configure container uses MakeGenericType with the container type. MakeGenericType + struct container type requires IsDynamicCodeSupported.
            if (containerType.IsValueType && !RuntimeFeature.IsDynamicCodeSupported)
            {
                throw new InvalidOperationException("A ValueType TContainerBuilder isn't supported with AOT.");
            }

            var actionType = typeof(Action<,>).MakeGenericType(typeof(HostBuilderContext), containerType);

            // Get the private ConfigureContainer method on this type then close over the container type
            var configureCallback = typeof(GenericWebHostBuilder).GetMethod(nameof(ConfigureContainerImpl), BindingFlags.NonPublic | BindingFlags.Instance)!
                                             .MakeGenericMethod(containerType)
                                             .CreateDelegate(actionType, genericWebHostBuilder);

            // _builder.ConfigureContainer<T>(ConfigureContainer);
            typeof(IHostBuilder).GetMethod(nameof(IHostBuilder.ConfigureContainer))!
                .MakeGenericMethod(containerType)
                .InvokeWithoutWrappingExceptions(genericWebHostBuilder._builder, new object[] { configureCallback });
        }
    }

    private void ConfigureContainerImpl<TContainer>(HostBuilderContext context, TContainer container) where TContainer : notnull
    {
        var instance = context.Properties[_startupKey];
        var builder = (ConfigureContainerBuilder)context.Properties[typeof(ConfigureContainerBuilder)];
        builder.Build(instance)(container);
    }

    public IWebHostBuilder Configure(Action<IApplicationBuilder> configure)
    {
        var startupAssemblyName = configure.GetMethodInfo().DeclaringType!.Assembly.GetName().Name!;

        UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName);

        // Clear the startup type
        _startupObject = configure;

        _builder.ConfigureServices((context, services) =>
        {
            if (object.ReferenceEquals(_startupObject, configure))
            {
                services.Configure<GenericWebHostServiceOptions>(options =>
                {
                    options.ConfigureApplication = configure;
                });
            }
        });

        return this;
    }

    public IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure)
    {
        var startupAssemblyName = configure.GetMethodInfo().DeclaringType!.Assembly.GetName().Name!;

        UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName);

        // Clear the startup type
        _startupObject = configure;

        _builder.ConfigureServices((context, services) =>
        {
            if (object.ReferenceEquals(_startupObject, configure))
            {
                services.Configure<GenericWebHostServiceOptions>(options =>
                {
                    var webhostBuilderContext = GetWebHostBuilderContext(context);
                    options.ConfigureApplication = app => configure(webhostBuilderContext, app);
                });
            }
        });

        return this;
    }

    // This exists just so that we can use ActivatorUtilities.CreateInstance on the Startup class
    private sealed class HostServiceProvider : IServiceProvider
    {
        private readonly WebHostBuilderContext _context;

        public HostServiceProvider(WebHostBuilderContext context)
        {
            _context = context;
        }

        public object? GetService(Type serviceType)
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
