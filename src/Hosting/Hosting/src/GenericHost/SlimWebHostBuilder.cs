// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class SlimWebHostBuilder : WebHostBuilderBase, ISupportsStartup
{
    public SlimWebHostBuilder(IHostBuilder builder, WebHostBuilderOptions options)
        : base(builder, options)
    {
        _builder.ConfigureHostConfiguration(config =>
        {
            config.AddConfiguration(_config);
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
        });
    }

    public IWebHostBuilder Configure(Action<IApplicationBuilder> configure)
    {
        throw new NotSupportedException();
    }

    public IWebHostBuilder UseStartup([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type startupType)
    {
        throw new NotSupportedException();
    }

    public IWebHostBuilder UseStartup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TStartup>(Func<WebHostBuilderContext, TStartup> startupFactory)
    {
        throw new NotSupportedException();
    }

    public IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure)
    {
        var startupAssemblyName = configure.GetMethodInfo().DeclaringType!.Assembly.GetName().Name!;

        UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName);

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
}
