// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Contains the methods to locate <c>_ViewStart.cshtml</c> 
    /// </summary>
    public static class ViewStartUtility
    {
        private const string ViewStartFileName = "_ViewStart.cshtml";

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
        /// <param name="applicationRelativePath">The application relative path of the file to locate
        /// <c>_ViewStart</c>s for.</param>
        /// <returns>A sequence of paths that represent potential view start locations.</returns>
        /// <remarks>
        /// This method returns paths starting from the directory of <paramref name="applicationRelativePath"/> and
        /// moves upwards until it hits the application root.
        /// e.g.
        /// /Views/Home/View.cshtml -> [ /Views/Home/_ViewStart.cshtml, /Views/_ViewStart.cshtml, /_ViewStart.cshtml ]
        /// </remarks>
        public static IEnumerable<string> GetViewStartLocations(string applicationRelativePath)
        {
            if (string.IsNullOrEmpty(applicationRelativePath))
            {
                return Enumerable.Empty<string>();
            }

            if (applicationRelativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                applicationRelativePath = applicationRelativePath.Substring(2);
            }

            if (applicationRelativePath.StartsWith("/", StringComparison.Ordinal))
            {
                applicationRelativePath = applicationRelativePath.Substring(1);
            }

            if (Path.IsPathRooted(applicationRelativePath))
            {
                // If the path looks like it's app relative, don't attempt to construct _ViewStart paths.
                return Enumerable.Empty<string>();
            }

            if (IsViewStart(applicationRelativePath))
            {
                // If the specified path is a ViewStart file, then the first view start that applies to it is the
                // parent view start.
                applicationRelativePath = Path.GetDirectoryName(applicationRelativePath);
                if (string.IsNullOrEmpty(applicationRelativePath))
                {
                    return Enumerable.Empty<string>();
                }
            }

            var viewStartLocations = new List<string>();
            while (!string.IsNullOrEmpty(applicationRelativePath))
            {
                applicationRelativePath = Path.GetDirectoryName(applicationRelativePath);
                var viewStartPath = Path.Combine(applicationRelativePath, ViewStartFileName);
                viewStartLocations.Add(viewStartPath);
            }

            return viewStartLocations;
        }
    }
}