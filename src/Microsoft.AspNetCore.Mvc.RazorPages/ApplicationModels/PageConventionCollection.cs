// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class PageConventionCollection : Collection<IPageConvention>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageConventionCollection"/> class that is empty.
        /// </summary>
        public PageConventionCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageConventionCollection"/> class
        /// as a wrapper for the specified list.
        /// </summary>
        /// <param name="conventions">The list that is wrapped by the new collection.</param>
        public PageConventionCollection(IList<IPageConvention> conventions)
            : base(conventions)
        {
        }

        /// <summary>
        /// Creates and adds an <see cref="IPageApplicationModelConvention"/> that invokes an action on the
        /// <see cref="PageApplicationModel"/> for the page with the speciifed name.
        /// </summary>
        /// <param name="pageName">The name of the page e.g. <c>/Users/List</c></param>
        /// <param name="action">The <see cref="Action"/>.</param>
        /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
        public IPageApplicationModelConvention AddPageApplicationModelConvention(
            string pageName,
            Action<PageApplicationModel> action)
        {
            EnsureValidPageName(pageName);

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return Add(new PageApplicationModelConvention(pageName, action));
        }

        /// <summary>
        /// Creates and adds an <see cref="IPageApplicationModelConvention"/> that invokes an action on
        /// <see cref="PageApplicationModel"/> instances for all page under the specified folder.
        /// </summary>
        /// <param name="folderPath">The path of the folder relative to the Razor Pages root. e.g. <c>/Users/</c></param>
        /// <param name="action">The <see cref="Action"/>.</param>
        /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
        public IPageApplicationModelConvention AddFolderApplicationModelConvention(string folderPath, Action<PageApplicationModel> action)
        {
            EnsureValidFolderPath(folderPath);

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return Add(new FolderApplicationModelConvention(folderPath, action));
        }

        /// <summary>
        /// Creates and adds an <see cref="IPageRouteModelConvention"/> that invokes an action on the
        /// <see cref="PageRouteModel"/> for the page with the speciifed name.
        /// </summary>
        /// <param name="pageName">The name of the page e.g. <c>/Users/List</c></param>
        /// <param name="action">The <see cref="Action"/>.</param>
        /// <returns>The added <see cref="IPageRouteModelConvention"/>.</returns>
        public IPageRouteModelConvention AddPageRouteModelConvention(string pageName, Action<PageRouteModel> action)
        {
            EnsureValidPageName(pageName);

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return Add(new PageRouteModelConvention(pageName, action));
        }

        /// <summary>
        /// Creates and adds an <see cref="IPageRouteModelConvention"/> that invokes an action on
        /// <see cref="PageRouteModel"/> instances for all page under the specified folder.
        /// </summary>
        /// <param name="folderPath">The path of the folder relative to the Razor Pages root. e.g. <c>/Users/</c></param>
        /// <param name="action">The <see cref="Action"/>.</param>
        /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
        public IPageRouteModelConvention AddFolderRouteModelConvention(string folderPath, Action<PageRouteModel> action)
        {
            EnsureValidFolderPath(folderPath);

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return Add(new FolderRouteModelConvention(folderPath, action));
        }

        /// <summary>
        /// Removes all <see cref="IPageConvention"/> instances of the specified type.
        /// </summary>
        /// <typeparam name="TPageConvention">The type to remove.</typeparam>
        public void RemoveType<TPageConvention>() where TPageConvention : IPageConvention
        {
            RemoveType(typeof(TPageConvention));
        }

        /// <summary>
        /// Removes all <see cref="IPageConvention"/> instances of the specified type.
        /// </summary>
        /// <param name="pageConventionType">The type to remove.</param>
        public void RemoveType(Type pageConventionType)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var pageConvention = this[i];
                if (pageConvention.GetType() == pageConventionType)
                {
                    RemoveAt(i);
                }
            }
        }

        // Internal for unit testing
        internal static void EnsureValidPageName(string pageName)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            if (pageName[0] != '/' || pageName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(Resources.FormatInvalidValidPageName(pageName), nameof(pageName));
            }
        }

        // Internal for unit testing
        internal static void EnsureValidFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(folderPath));
            }

            if (folderPath[0] != '/')
            {
                throw new ArgumentException(Resources.PathMustBeRootRelativePath, nameof(folderPath));
            }
        }

        private TConvention Add<TConvention>(TConvention convention) where TConvention: IPageConvention
        {
            base.Add(convention);
            return convention;
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

        // Internal for unit testing
        internal static bool PathBelongsToFolder(string folderPath, string viewEnginePath)
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
