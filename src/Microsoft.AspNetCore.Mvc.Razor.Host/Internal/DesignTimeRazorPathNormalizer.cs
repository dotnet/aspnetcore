// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DesignTimeRazorPathNormalizer : RazorPathNormalizer
    {
        private readonly string _applicationRoot;

        public DesignTimeRazorPathNormalizer(string applicationRoot)
        {
            if (applicationRoot == null)
            {
                throw new ArgumentNullException(nameof(applicationRoot));
            }

            _applicationRoot = applicationRoot;
        }

        public override string NormalizePath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // Need to convert path to application relative (rooted paths are passed in during design time).
            if (Path.IsPathRooted(path) && path.StartsWith(_applicationRoot, StringComparison.Ordinal))
            {
                path = path.Substring(_applicationRoot.Length);
            }

            return path;
        }
    }
}