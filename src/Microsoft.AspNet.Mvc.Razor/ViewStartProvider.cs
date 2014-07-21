// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <inheritdoc />
    public class ViewStartProvider : IViewStartProvider
    {
        private const string ViewStartFileName = "_ViewStart.cshtml";
        private readonly string _appRoot;
        private readonly IRazorPageFactory _pageFactory;

        public ViewStartProvider(IApplicationEnvironment appEnv,
                                 IRazorPageFactory pageFactory)
        {
            _appRoot = TrimTrailingSlash(appEnv.ApplicationBasePath);
            _pageFactory = pageFactory;
        }

        /// <inheritdoc />
        public IEnumerable<IRazorPage> GetViewStartPages([NotNull] string path)
        {
            var viewStartLocations = GetViewStartLocations(path);
            var viewStarts = viewStartLocations.Select(_pageFactory.CreateInstance)
                                                .Where(p => p != null)
                                                .ToArray();

            // GetViewStartLocations return ViewStarts inside-out that is the _ViewStart closest to the page 
            // is the first: e.g. [ /Views/Home/_ViewStart, /Views/_ViewStart, /_ViewStart ]
            // However they need to be executed outside in, so we'll reverse the sequence.
            Array.Reverse(viewStarts);

            return viewStarts;
        }

        internal IEnumerable<string> GetViewStartLocations(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Enumerable.Empty<string>();
            }

            var viewStartLocations = new List<string>();
            var currentDir = GetViewDirectory(_appRoot, path);
            while (IsSubDirectory(_appRoot, currentDir))
            {
                viewStartLocations.Add(Path.Combine(currentDir, ViewStartFileName));
                currentDir = Path.GetDirectoryName(currentDir);
            }

            return viewStartLocations;
        }

        private static bool IsSubDirectory(string appRoot, string currentDir)
        {
            return currentDir.StartsWith(appRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetViewDirectory(string appRoot, string viewPath)
        {
            if (viewPath.StartsWith("~/"))
            {
                viewPath = viewPath.Substring(2);
            }
            else if (viewPath[0] == Path.DirectorySeparatorChar || 
                     viewPath[0] == Path.AltDirectorySeparatorChar)
            {
                viewPath = viewPath.Substring(1);
            }

            var viewDir = Path.GetDirectoryName(viewPath);
            return Path.GetFullPath(Path.Combine(appRoot, viewDir));
        }

        private static string TrimTrailingSlash(string path)
        {
            if (path.Length > 0 &&
                path[path.Length - 1] == Path.DirectorySeparatorChar)
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }
    }
}