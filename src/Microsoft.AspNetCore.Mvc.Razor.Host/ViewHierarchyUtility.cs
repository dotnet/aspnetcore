// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Contains methods to locate <c>_ViewStart.cshtml</c> and <c>_ViewImports.cshtml</c>
    /// </summary>
    public static class ViewHierarchyUtility
    {
        private const string ViewStartFileName = "_ViewStart.cshtml";

        /// <summary>
        /// File name of <c>_ViewImports.cshtml</c> file
        /// </summary>
        public static readonly string ViewImportsFileName = "_ViewImports.cshtml";

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
            return GetHierarchicalPath(applicationRelativePath, ViewStartFileName);
        }

        /// <summary>
        /// Gets the locations for <c>_ViewImports</c>s that are applicable to the specified path.
        /// </summary>
        /// <param name="applicationRelativePath">The application relative path of the file to locate
        /// <c>_ViewImports</c>s for.</param>
        /// <returns>A sequence of paths that represent potential <c>_ViewImports</c> locations.</returns>
        /// <remarks>
        /// This method returns paths starting from the directory of <paramref name="applicationRelativePath"/> and
        /// moves upwards until it hits the application root.
        /// e.g.
        /// /Views/Home/View.cshtml -> [ /Views/Home/_ViewImports.cshtml, /Views/_ViewImports.cshtml,
        ///                              /_ViewImports.cshtml ]
        /// </remarks>
        public static IEnumerable<string> GetViewImportsLocations(string applicationRelativePath)
        {
            return GetHierarchicalPath(applicationRelativePath, ViewImportsFileName);
        }

        private static IEnumerable<string> GetHierarchicalPath(string relativePath, string fileName)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return Enumerable.Empty<string>();
            }

            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                relativePath = relativePath.Substring(2);
            }

            if (relativePath.StartsWith("/", StringComparison.Ordinal))
            {
                relativePath = relativePath.Substring(1);
            }

            if (string.Equals(Path.GetFileName(relativePath), fileName, StringComparison.OrdinalIgnoreCase))
            {
                // If the specified path is for the file hierarchy being constructed, then the first file that applies
                // to it is in a parent directory.
                relativePath = Path.GetDirectoryName(relativePath);

                if (string.IsNullOrEmpty(relativePath))
                {
                    return Enumerable.Empty<string>();
                }
            }

            var builder = new StringBuilder(relativePath);
            builder.Replace('\\', '/');

            if (builder.Length > 0 && builder[0] != '/')
            {
                builder.Insert(0, '/');
            }

            var locations = new List<string>();
            for (var index = builder.Length - 1; index >= 0; index--)
            {
                if (builder[index] == '/')
                {
                    builder.Length = index + 1;
                    builder.Append(fileName);

                    locations.Add(builder.ToString());
                }
            }

            return locations;
        }
    }
}