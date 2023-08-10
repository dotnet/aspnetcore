// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.HotReload;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up MVC services in an <see cref="IServiceCollection" />.
/// </summary>
public static class MvcServiceCollectionExtensions
{
    /// <summary>
    /// Adds MVC services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    [RequiresUnreferencedCode("MVC does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddMvc(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddControllersWithViews();
        return services.AddRazorPages();
    }

    /// <summary>
    /// Adds MVC services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    [RequiresUnreferencedCode("MVC does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddMvc(this IServiceCollection services, Action<MvcOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);

        var builder = services.AddMvc();
        builder.Services.Configure(setupAction);

        return builder;
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for views or pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers for an API. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("MVC does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddControllers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = AddControllersCore(services);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for views or pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers for an API. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("MVC does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddControllers(this IServiceCollection services, Action<MvcOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        // This method excludes all of the view-related services by default.
        var builder = AddControllersCore(services);
        if (configure != null)
        {
            builder.AddMvcOptions(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    private static IMvcCoreBuilder AddControllersCore(IServiceCollection services)
    {
        // This method excludes all of the view-related services by default.
        var builder = services
            .AddMvcCore()
            .AddApiExplorer()
            .AddAuthorization()
            .AddCors()
            .AddDataAnnotations()
            .AddFormatterMappings();

        if (MetadataUpdater.IsSupported)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HotReloadService>());
        }

        return builder;
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers with views. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// <see cref="MvcViewFeaturesMvcCoreBuilderExtensions.AddViews(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorMvcCoreBuilderExtensions.AddRazorViewEngine(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("MVC does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddControllersWithViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = AddControllersWithViewsCore(services);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers with views. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// <see cref="MvcViewFeaturesMvcCoreBuilderExtensions.AddViews(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorMvcCoreBuilderExtensions.AddRazorViewEngine(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("MVC does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddControllersWithViews(this IServiceCollection services, Action<MvcOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        // This method excludes all of the view-related services by default.
        var builder = AddControllersWithViewsCore(services);
        if (configure != null)
        {
            builder.AddMvcOptions(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    private static IMvcCoreBuilder AddControllersWithViewsCore(IServiceCollection services)
    {
        var builder = AddControllersCore(services)
            .AddViews()
            .AddRazorViewEngine()
            .AddCacheTagHelper();

        AddTagHelpersFrameworkParts(builder.PartManager);

        return builder;
    }

    /// <summary>
    /// Adds services for pages to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features for pages. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorPagesMvcCoreBuilderExtensions.AddRazorPages(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers for APIs call <see cref="AddControllers(IServiceCollection)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("Razor Pages does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddRazorPages(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = AddRazorPagesCore(services);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for pages to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features for pages. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorPagesMvcCoreBuilderExtensions.AddRazorPages(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers for APIs call <see cref="AddControllers(IServiceCollection)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("Razor Pages does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IMvcBuilder AddRazorPages(this IServiceCollection services, Action<RazorPagesOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = AddRazorPagesCore(services);
        if (configure != null)
        {
            builder.AddRazorPages(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    private static IMvcCoreBuilder AddRazorPagesCore(IServiceCollection services)
    {
        // This method includes the minimal things controllers need. It's not really feasible to exclude the services
        // for controllers.
        var builder = services
            .AddMvcCore()
            .AddAuthorization()
            .AddDataAnnotations()
            .AddRazorPages()
            .AddCacheTagHelper();

        AddTagHelpersFrameworkParts(builder.PartManager);

        if (MetadataUpdater.IsSupported)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HotReloadService>());
        }

        return builder;
    }

    internal static void AddTagHelpersFrameworkParts(ApplicationPartManager partManager)
    {
        var mvcTagHelpersAssembly = typeof(InputTagHelper).Assembly;
        if (!partManager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == mvcTagHelpersAssembly))
        {
            partManager.ApplicationParts.Add(new FrameworkAssemblyPart(mvcTagHelpersAssembly));
        }

        var mvcRazorAssembly = typeof(UrlResolutionTagHelper).Assembly;
        if (!partManager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == mvcRazorAssembly))
        {
            partManager.ApplicationParts.Add(new FrameworkAssemblyPart(mvcRazorAssembly));
        }
    }

    [DebuggerDisplay("{Name}")]
    private sealed class FrameworkAssemblyPart : AssemblyPart, ICompilationReferencesProvider
    {
        public FrameworkAssemblyPart(Assembly assembly)
            : base(assembly)
        {
        }

        IEnumerable<string> ICompilationReferencesProvider.GetReferencePaths() => Enumerable.Empty<string>();
    }
}
