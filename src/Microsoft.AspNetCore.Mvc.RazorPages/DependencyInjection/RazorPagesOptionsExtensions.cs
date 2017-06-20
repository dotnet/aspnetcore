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
    /// Extensions for <see cref="RazorPagesOptions"/>.
    /// </summary>
    public static class RazorPagesOptionsExtensions
    {
        /// <summary>
        /// Configures the specified <paramref name="factory"/> to apply filters to all Razor Pages.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="factory">The factory to create filters.</param>
        /// <returns></returns>
        public static RazorPagesOptions ConfigureFilter(this RazorPagesOptions options, Func<PageApplicationModel, IFilterMetadata> factory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            options.ApplicationModelConventions.Add(new FolderApplicationModelConvention("/", model => model.Filters.Add(factory(model))));
            return options;
        }

        /// <summary>
        /// Configures the specified <paramref name="filter"/> to apply to all Razor Pages.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="filter">The <see cref="IFilterMetadata"/> to add.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions ConfigureFilter(this RazorPagesOptions options, IFilterMetadata filter)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            options.ApplicationModelConventions.Add(new FolderApplicationModelConvention("/", model => model.Filters.Add(filter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AllowAnonymousFilter"/> to the page with the specified name.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="pageName">The page name.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AllowAnonymousToPage(this RazorPagesOptions options, string pageName)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            var anonymousFilter = new AllowAnonymousFilter();
            options.ApplicationModelConventions.Add(new PageApplicationModelConvention(pageName, model => model.Filters.Add(anonymousFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AllowAnonymousFilter"/> to all pages under the specified folder.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AllowAnonymousToFolder(this RazorPagesOptions options, string folderPath)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(folderPath));
            }

            var anonymousFilter = new AllowAnonymousFilter();
            options.ApplicationModelConventions.Add(new FolderApplicationModelConvention(folderPath, model => model.Filters.Add(anonymousFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> with the specified policy to the page with the specified name.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="pageName">The page name.</param>
        /// <param name="policy">The authorization policy.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AuthorizePage(this RazorPagesOptions options, string pageName, string policy)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            var authorizeFilter = new AuthorizeFilter(policy);
            options.ApplicationModelConventions.Add(new PageApplicationModelConvention(pageName, model => model.Filters.Add(authorizeFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> to the page with the specified name.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="pageName">The page name.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AuthorizePage(this RazorPagesOptions options, string pageName) =>
            AuthorizePage(options, pageName, policy: string.Empty);

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> with the specified policy to all pages under the specified folder.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="policy">The authorization policy.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AuthorizeFolder(this RazorPagesOptions options, string folderPath, string policy)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(folderPath));
            }

            var authorizeFilter = new AuthorizeFilter(policy);
            options.ApplicationModelConventions.Add(new FolderApplicationModelConvention(folderPath, model => model.Filters.Add(authorizeFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> to all pages under the specified folder.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AuthorizeFolder(this RazorPagesOptions options, string folderPath) =>
            AuthorizeFolder(options, folderPath, policy: string.Empty);

        /// <summary>
        /// Adds the specified <paramref name="route"/> to the page at the specified <paramref name="pageName"/>.
        /// <para>
        /// The page can be routed via <paramref name="route"/> in addition to the default set of path based routes.
        /// All links generated for this page will use the specified route.
        /// </para>
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/>.</param>
        /// <param name="pageName">The page name.</param>
        /// <param name="route">The route to associate with the page.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AddPageRoute(this RazorPagesOptions options, string pageName, string route)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            options.RouteModelConventions.Add(new PageRouteModelConvention(pageName, model =>
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
            }));

            return options;
        }

        private class PageRouteModelConvention : IPageRouteModelConvention
        {
            private readonly string _path;
            private readonly Action<PageRouteModel> _action;

            public PageRouteModelConvention(string path, Action<PageRouteModel> action)
            {
                _path = path;
                _action = action;
            }

            public void Apply(PageRouteModel model)
            {
                if (string.Equals(model.ViewEnginePath, _path, StringComparison.OrdinalIgnoreCase))
                {
                    _action(model);
                }
            }
        }

        private class FolderRouteModelConvention : IPageRouteModelConvention
        {
            private readonly string _folderPath;
            private readonly Action<PageRouteModel> _action;

            public FolderRouteModelConvention(string folderPath, Action<PageRouteModel> action)
            {
                _folderPath = folderPath.TrimEnd('/');
                _action = action;
            }

            public void Apply(PageRouteModel model)
            {
                var viewEnginePath = model.ViewEnginePath;

                if (PathBelongsToFolder(_folderPath, viewEnginePath))
                {
                    _action(model);
                }
            }
        }

        private class PageApplicationModelConvention : IPageApplicationModelConvention
        {
            private readonly string _path;
            private readonly Action<PageApplicationModel> _action;

            public PageApplicationModelConvention(string path, Action<PageApplicationModel> action)
            {
                _path = path;
                _action = action;
            }

            public void Apply(PageApplicationModel model)
            {
                if (string.Equals(model.ViewEnginePath, _path, StringComparison.OrdinalIgnoreCase))
                {
                    _action(model);
                }
            }
        }

        private class FolderApplicationModelConvention : IPageApplicationModelConvention
        {
            private readonly string _folderPath;
            private readonly Action<PageApplicationModel> _action;

            public FolderApplicationModelConvention(string folderPath, Action<PageApplicationModel> action)
            {
                _folderPath = folderPath.TrimEnd('/');
                _action = action;
            }

            public void Apply(PageApplicationModel model)
            {
                var viewEnginePath = model.ViewEnginePath;

                if (PathBelongsToFolder(_folderPath, viewEnginePath))
                {
                    _action(model);
                }
            }
        }

        private static bool PathBelongsToFolder(string folderPath, string viewEnginePath)
        {
            if (folderPath == "/")
            {
                // Root directory covers everything.
                return true;
            }

            return viewEnginePath.Length > folderPath.Length &&
                viewEnginePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase) &&
                viewEnginePath[folderPath.Length] == '/';
        }
    }
}
