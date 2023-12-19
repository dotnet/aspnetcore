// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for <see cref="PageConventionCollection"/>.
/// </summary>
public static class PageConventionCollectionExtensions
{
    /// <summary>
    /// Configures the specified <paramref name="factory"/> to apply filters to all Razor Pages.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="factory">The factory to create filters.</param>
    /// <returns></returns>
    public static IPageApplicationModelConvention ConfigureFilter(
        this PageConventionCollection conventions,
        Func<PageApplicationModel, IFilterMetadata> factory)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(factory);

        return conventions.AddFolderApplicationModelConvention("/", model => model.Filters.Add(factory(model)));
    }

    /// <summary>
    /// Configures the specified <paramref name="filter"/> to apply to all Razor Pages.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/> to add.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection ConfigureFilter(this PageConventionCollection conventions, IFilterMetadata filter)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(filter);

        conventions.AddFolderApplicationModelConvention("/", model => model.Filters.Add(filter));
        return conventions;
    }

    /// <summary>
    /// Adds the specified <paramref name="convention"/> to <paramref name="conventions"/>.
    /// The added convention will apply to all handler properties and parameters on handler methods.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="convention">The <see cref="IParameterModelBaseConvention"/> to apply.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection Add(this PageConventionCollection conventions, IParameterModelBaseConvention convention)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(convention);

        var adapter = new ParameterModelBaseConventionAdapter(convention);
        conventions.Add(adapter);
        return conventions;
    }

    /// <summary>
    /// Allows anonymous access to the page with the specified name.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="pageName">The page name.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AllowAnonymousToPage(this PageConventionCollection conventions, string pageName)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(pageName);

        conventions.AddPageApplicationModelConvention(pageName, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AllowAnonymousAttribute());
            }
            else
            {
                model.Filters.Add(new AllowAnonymousFilter());
            }
        });
        return conventions;
    }

    /// <summary>
    /// Allows anonymous access to the page with the specified name located in the specified area.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">
    /// The page name e.g. <c>/Users/List</c>
    /// <para>
    /// The page name is the path of the file without extension, relative to the pages root directory for the specified area.
    /// e.g. the page name for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
    /// </para>
    /// </param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AllowAnonymousToAreaPage(
        this PageConventionCollection conventions,
        string areaName,
        string pageName)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(areaName);
        ArgumentException.ThrowIfNullOrEmpty(pageName);

        conventions.AddAreaPageApplicationModelConvention(areaName, pageName, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AllowAnonymousAttribute());
            }
            else
            {
                model.Filters.Add(new AllowAnonymousFilter());
            }
        });
        return conventions;
    }

    /// <summary>
    /// Allows anonymous access to all pages under the specified folder.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="folderPath">The folder path.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AllowAnonymousToFolder(this PageConventionCollection conventions, string folderPath)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(folderPath);

        conventions.AddFolderApplicationModelConvention(folderPath, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AllowAnonymousAttribute());
            }
            else
            {
                model.Filters.Add(new AllowAnonymousFilter());
            }
        });
        return conventions;
    }

    /// <summary>
    /// Allows anonymous access to all pages under the specified area folder.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="folderPath">
    /// The folder path e.g. <c>/Manage/</c>
    /// <para>
    /// The folder path is the path of the folder, relative to the pages root directory for the specified area.
    /// e.g. the folder path for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage</c>.
    /// </para>
    ///.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AllowAnonymousToAreaFolder(
        this PageConventionCollection conventions,
        string areaName,
        string folderPath)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(areaName);
        ArgumentException.ThrowIfNullOrEmpty(folderPath);

        conventions.AddAreaFolderApplicationModelConvention(areaName, folderPath, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AllowAnonymousAttribute());
            }
            else
            {
                model.Filters.Add(new AllowAnonymousFilter());
            }
        });
        return conventions;
    }

    /// <summary>
    /// Requires authorization with the specified policy for the page with the specified name.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="pageName">The page name.</param>
    /// <param name="policy">The authorization policy.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizePage(this PageConventionCollection conventions, string pageName, string policy)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(pageName);

        conventions.AddPageApplicationModelConvention(pageName, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AuthorizeAttribute(policy));
            }
            else
            {
                model.Filters.Add(new AuthorizeFilter(policy));
            }
        });
        return conventions;
    }

    /// <summary>
    /// Requires authorization for the specified page.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="pageName">The page name.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizePage(this PageConventionCollection conventions, string pageName) =>
        AuthorizePage(conventions, pageName, policy: string.Empty);

    /// <summary>
    /// Requires authorization for the specified area page.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">
    /// The page name e.g. <c>/Users/List</c>
    /// <para>
    /// The page name is the path of the file without extension, relative to the pages root directory for the specified area.
    /// e.g. the page name for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
    /// </para>
    /// </param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizeAreaPage(this PageConventionCollection conventions, string areaName, string pageName)
        => AuthorizeAreaPage(conventions, areaName, pageName, policy: string.Empty);

    /// <summary>
    /// Requires authorization for the specified area page with the specified policy.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">
    /// The page name e.g. <c>/Users/List</c>
    /// <para>
    /// The page name is the path of the file without extension, relative to the pages root directory for the specified area.
    /// e.g. the page name for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
    /// </para>
    /// </param>
    /// <param name="policy">The authorization policy.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizeAreaPage(
        this PageConventionCollection conventions,
        string areaName,
        string pageName,
        string policy)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(areaName);
        ArgumentException.ThrowIfNullOrEmpty(pageName);

        conventions.AddAreaPageApplicationModelConvention(areaName, pageName, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AuthorizeAttribute(policy));
            }
            else
            {
                model.Filters.Add(new AuthorizeFilter(policy));
            }
        });
        return conventions;
    }

    /// <summary>
    /// Requires authorization for all pages under the specified folder.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="folderPath">The folder path.</param>
    /// <param name="policy">The authorization policy.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizeFolder(this PageConventionCollection conventions, string folderPath, string policy)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(folderPath);

        conventions.AddFolderApplicationModelConvention(folderPath, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AuthorizeAttribute(policy));
            }
            else
            {
                model.Filters.Add(new AuthorizeFilter(policy));
            }
        });
        return conventions;
    }

    /// <summary>
    /// Requires authorization for all pages under the specified folder.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="folderPath">The folder path.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizeFolder(this PageConventionCollection conventions, string folderPath) =>
        AuthorizeFolder(conventions, folderPath, policy: string.Empty);

    /// <summary>
    /// Requires authorization with the default policy for all pages under the specified folder.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="folderPath">
    /// The folder path e.g. <c>/Manage/</c>
    /// <para>
    /// The folder path is the path of the folder, relative to the pages root directory for the specified area.
    /// e.g. the folder path for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage</c>.
    /// </para>
    /// </param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizeAreaFolder(this PageConventionCollection conventions, string areaName, string folderPath)
        => AuthorizeAreaFolder(conventions, areaName, folderPath, policy: string.Empty);

    /// <summary>
    /// Requires authorization with the specified policy for all pages under the specified folder.
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="folderPath">
    /// The folder path e.g. <c>/Manage/</c>
    /// <para>
    /// The folder path is the path of the folder, relative to the pages root directory for the specified area.
    /// e.g. the folder path for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage</c>.
    /// </para>
    /// </param>
    /// <param name="policy">The authorization policy.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AuthorizeAreaFolder(
        this PageConventionCollection conventions,
        string areaName,
        string folderPath,
        string policy)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(areaName);
        ArgumentException.ThrowIfNullOrEmpty(folderPath);

        conventions.AddAreaFolderApplicationModelConvention(areaName, folderPath, model =>
        {
            if (conventions.MvcOptions.EnableEndpointRouting)
            {
                model.EndpointMetadata.Add(new AuthorizeAttribute(policy));
            }
            else
            {
                model.Filters.Add(new AuthorizeFilter(policy));
            }
        });
        return conventions;
    }

    /// <summary>
    /// Adds the specified <paramref name="route"/> to the page at the specified <paramref name="pageName"/>.
    /// <para>
    /// The page can be routed via <paramref name="route"/> in addition to the default set of path based routes.
    /// All links generated for this page will use the specified route.
    /// </para>
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/>.</param>
    /// <param name="pageName">The page name.</param>
    /// <param name="route">The route to associate with the page.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AddPageRoute(this PageConventionCollection conventions, string pageName, [StringSyntax("Route")] string route)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(pageName);
        ArgumentNullException.ThrowIfNull(route);

        conventions.AddPageRouteModelConvention(pageName, AddPageRouteThunk(route));

        return conventions;
    }

    /// <summary>
    /// Adds the specified <paramref name="route"/> to the page at the specified <paramref name="pageName"/> located in the specified
    /// area.
    /// <para>
    /// The page can be routed via <paramref name="route"/> in addition to the default set of path based routes.
    /// All links generated for this page will use the specified route.
    /// </para>
    /// </summary>
    /// <param name="conventions">The <see cref="PageConventionCollection"/>.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">
    /// The page name e.g. <c>/Users/List</c>
    /// <para>
    /// The page name is the path of the file without extension, relative to the pages root directory for the specified area.
    /// e.g. the page name for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
    /// </para>
    /// </param>
    /// <param name="route">The route to associate with the page.</param>
    /// <returns>The <see cref="PageConventionCollection"/>.</returns>
    public static PageConventionCollection AddAreaPageRoute(
        this PageConventionCollection conventions,
        string areaName,
        string pageName,
        [StringSyntax("Route")] string route)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentException.ThrowIfNullOrEmpty(areaName);
        ArgumentException.ThrowIfNullOrEmpty(pageName);
        ArgumentNullException.ThrowIfNull(route);

        conventions.AddAreaPageRouteModelConvention(areaName, pageName, AddPageRouteThunk(route));

        return conventions;
    }

    private static Action<PageRouteModel> AddPageRouteThunk(string route)
    {
        return model =>
        {
            // Use the route specified in MapPageRoute for outbound routing.
            foreach (var selector in model.Selectors)
            {
                selector.AttributeRouteModel!.SuppressLinkGeneration = true;
            }

            model.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = route,
                }
            });
        };
    }

    private sealed class ParameterModelBaseConventionAdapter : IPageConvention, IParameterModelBaseConvention
    {
        private readonly IParameterModelBaseConvention _convention;

        public ParameterModelBaseConventionAdapter(IParameterModelBaseConvention convention)
        {
            _convention = convention;
        }

        public void Apply(ParameterModelBase parameter)
        {
            _convention.Apply(parameter);
        }
    }
}
