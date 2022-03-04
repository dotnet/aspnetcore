// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal static class RazorFileHierarchy
    {
        private const string ViewStartFileName = "_ViewStart.cshtml";

        public static IEnumerable<string> GetViewStartPaths(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(path));
            }

            if (path[0] != '/')
            {
                throw new ArgumentException(Resources.RazorProject_PathMustStartWithForwardSlash, nameof(path));
            }

            if (path.Length == 1)
            {
                yield break;
            }

            var builder = new StringBuilder(path);
            var maxIterations = 255;
            var index = path.Length;
            while (maxIterations-- > 0 && index > 1 && (index = path.LastIndexOf('/', index - 1)) != -1)
            {
                builder.Length = index + 1;
                builder.Append(ViewStartFileName);

                var itemPath = builder.ToString();
                yield return itemPath;
            }
        }
    }
}
