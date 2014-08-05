// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNet.Mvc.Razor
{
    public static class ViewStartUtility
    {
        private const string ViewStartFileName = "_viewstart.cshtml";

        /// <summary>
        /// Determines if the given path represents a view start file.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>True if the path is a view start file, false otherwise.</returns>
        public static bool IsViewStart([NotNull] string path)
        {
            var fileName = Path.GetFileName(path);
            return string.Equals(ViewStartFileName, fileName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the view start locations that are applicable to the specified path.
        /// </summary>
        /// <param name="applicationBase">The base of the application.</param>
        /// <param name="path">The path to locate view starts for.</param>
        /// <returns>A sequence of paths that represent potential view start locations.</returns>
        /// <remarks>
        /// This method returns paths starting from the directory of <paramref name="path"/> and moves
        /// upwards until it hits the application root.
        /// e.g.
        /// /Views/Home/View.cshtml -> [ /Views/Home/_ViewStart.cshtml, /Views/_ViewStart.cshtml, /_ViewStart.cshtml ]
        /// </remarks>
        public static IEnumerable<string> GetViewStartLocations(string applicationBase, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Enumerable.Empty<string>();
            }

            applicationBase = TrimTrailingSlash(applicationBase);
            var viewStartLocations = new List<string>();
            var currentDir = GetViewDirectory(applicationBase, path);
            while (IsSubDirectory(applicationBase, currentDir))
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