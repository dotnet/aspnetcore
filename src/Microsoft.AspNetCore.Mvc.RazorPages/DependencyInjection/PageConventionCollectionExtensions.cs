// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.Extensions.DependencyInjection
{
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
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

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
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            conventions.AddFolderApplicationModelConvention("/", model => model.Filters.Add(filter));
            return conventions;
        }

        /// <summary>
        /// Adds a <see cref="AllowAnonymousFilter"/> to the page with the specified name.
        /// </summary>
        /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
        /// <param name="pageName">The page name.</param>
        /// <returns>The <see cref="PageConventionCollection"/>.</returns>
        public static PageConventionCollection AllowAnonymousToPage(this PageConventionCollection conventions, string pageName)
        {
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            var anonymousFilter = new AllowAnonymousFilter();
            conventions.AddPageApplicationModelConvention(pageName, model => model.Filters.Add(anonymousFilter));
            return conventions;
        }

        /// <summary>
        /// Adds a <see cref="AllowAnonymousFilter"/> to all pages under the specified folder.
        /// </summary>
        /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <returns>The <see cref="PageConventionCollection"/>.</returns>
        public static PageConventionCollection AllowAnonymousToFolder(this PageConventionCollection conventions, string folderPath)
        {
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(folderPath));
            }

            var anonymousFilter = new AllowAnonymousFilter();
            conventions.AddFolderApplicationModelConvention(folderPath, model => model.Filters.Add(anonymousFilter));
            return conventions;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> with the specified policy to the page with the specified name.
        /// </summary>
        /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
        /// <param name="pageName">The page name.</param>
        /// <param name="policy">The authorization policy.</param>
        /// <returns>The <see cref="PageConventionCollection"/>.</returns>
        public static PageConventionCollection AuthorizePage(this PageConventionCollection conventions, string pageName, string policy)
        {
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            var authorizeFilter = new AuthorizeFilter(policy);
            conventions.AddPageApplicationModelConvention(pageName, model => model.Filters.Add(authorizeFilter));
            return conventions;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> to the page with the specified name.
        /// </summary>
        /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
        /// <param name="pageName">The page name.</param>
        /// <returns>The <see cref="PageConventionCollection"/>.</returns>
        public static PageConventionCollection AuthorizePage(this PageConventionCollection conventions, string pageName) =>
            AuthorizePage(conventions, pageName, policy: string.Empty);

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> with the specified policy to all pages under the specified folder.
        /// </summary>
        /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="policy">The authorization policy.</param>
        /// <returns>The <see cref="PageConventionCollection"/>.</returns>
        public static PageConventionCollection AuthorizeFolder(this PageConventionCollection conventions, string folderPath, string policy)
        {
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(folderPath));
            }

            var authorizeFilter = new AuthorizeFilter(policy);
            conventions.AddFolderApplicationModelConvention(folderPath, model => model.Filters.Add(authorizeFilter));
            return conventions;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> to all pages under the specified folder.
        /// </summary>
        /// <param name="conventions">The <see cref="PageConventionCollection"/> to configure.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <returns>The <see cref="PageConventionCollection"/>.</returns>
        public static PageConventionCollection AuthorizeFolder(this PageConventionCollection conventions, string folderPath) =>
            AuthorizeFolder(conventions, folderPath, policy: string.Empty);

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
        public static PageConventionCollection AddPageRoute(this PageConventionCollection conventions, string pageName, string route)
        {
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            conventions.AddPageRouteModelConvention(pageName, model =>
            {
                // Use the route specified in MapPageRoute for outbound routing.
                foreach (var selector in model.Selectors)
                {
                    selector.AttributeRouteModel.SuppressLinkGeneration = true;
                }

                model.Selectors.Add(new SelectorModel
                {
                    AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = route,
                    }
                });
            });

            return conventions;
        }
    }
}
