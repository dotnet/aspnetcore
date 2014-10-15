// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileSystems;

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
        public static IEnumerable<string> GetViewStartLocations(IFileSystem fileSystem, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Enumerable.Empty<string>();
            }
            if (path.StartsWith("~/", StringComparison.Ordinal))
            {
                path = path.Substring(1);
            }

            if (string.Equals(ViewStartFileName, Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
            {
                // If the specified path is a ViewStart file, then the first view start that applies to it is the
                // parent view start.
                if (!fileSystem.TryGetParentPath(path, out path))
                {
                    return Enumerable.Empty<string>();
                }
            }

            var viewStartLocations = new List<string>();

            while (fileSystem.TryGetParentPath(path, out path))
            {
                var viewStartPath = Path.Combine(path, ViewStartFileName);
                viewStartLocations.Add(viewStartPath);
            }

            return viewStartLocations;
        }
    }
}