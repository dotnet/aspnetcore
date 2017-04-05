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

            options.Conventions.Add(new FolderConvention("/", model => model.Filters.Add(factory(model))));
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

            options.Conventions.Add(new FolderConvention("/", model => model.Filters.Add(filter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AllowAnonymousFilter"/> to the page with the specified path.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="path">The path of the Razor Page.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AllowAnonymousToPage(this RazorPagesOptions options, string path)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(path));
            }

            var anonymousFilter = new AllowAnonymousFilter();
            options.Conventions.Add(new PageConvention(path, model => model.Filters.Add(anonymousFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AllowAnonymousFilter"/> to all pages under the specified path.
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
            options.Conventions.Add(new FolderConvention(folderPath, model => model.Filters.Add(anonymousFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> with the specified policy to the page with the specified path.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="path">The path of the Razor Page.</param>
        /// <param name="policy">The authorization policy.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AuthorizePage(this RazorPagesOptions options, string path, string policy)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(path));
            }

            var authorizeFilter = new AuthorizeFilter(policy);
            options.Conventions.Add(new PageConvention(path, model => model.Filters.Add(authorizeFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> to the page with the specified path.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="path">The path of the Razor Page.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AuthorizePage(this RazorPagesOptions options, string path) =>
            AuthorizePage(options, path, policy: string.Empty);

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> with the specified policy to all pages under the specified path.
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
            options.Conventions.Add(new FolderConvention(folderPath, model => model.Filters.Add(authorizeFilter)));
            return options;
        }

        /// <summary>
        /// Adds a <see cref="AuthorizeFilter"/> to all pages under the specified path.
        /// </summary>
        /// <param name="options">The <see cref="RazorPagesOptions"/> to configure.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <returns>The <see cref="RazorPagesOptions"/>.</returns>
        public static RazorPagesOptions AuthorizeFolder(this RazorPagesOptions options, string folderPath) =>
            AuthorizeFolder(options, folderPath, policy: string.Empty);

        private class PageConvention : IPageApplicationModelConvention
        {
            private readonly string _path;
            private readonly Action<PageApplicationModel> _action;

            public PageConvention(string path, Action<PageApplicationModel> action)
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

        private class FolderConvention : IPageApplicationModelConvention
        {
            private readonly string _folderPath;
            private readonly Action<PageApplicationModel> _action;

            public FolderConvention(string folderPath, Action<PageApplicationModel> action)
            {
                _folderPath = folderPath.TrimEnd('/');
                _action = action;
            }

            public void Apply(PageApplicationModel model)
            {
                var viewEnginePath = model.ViewEnginePath;

                var applyConvention = _folderPath == "/" ||
                    (viewEnginePath.Length > _folderPath.Length &&
                    viewEnginePath.StartsWith(_folderPath, StringComparison.OrdinalIgnoreCase) &&
                    viewEnginePath[_folderPath.Length] == '/');

                if (applyConvention)
                {
                    _action(model);
                }
            }
        }
    }
}
