// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Internal
{
    public class DesignTimeRazorPathNormalizer : RazorPathNormalizer
    {
        private readonly string _applicationRoot;

        public DesignTimeRazorPathNormalizer([NotNull] string applicationRoot)
        {
            _applicationRoot = applicationRoot;
        }

        public override string NormalizePath([NotNull] string path)
        {
            // Need to convert path to application relative (rooted paths are passed in during design time).
            if (Path.IsPathRooted(path) && path.StartsWith(_applicationRoot, StringComparison.Ordinal))
            {
                path = path.Substring(_applicationRoot.Length);
            }

            return path;
        }
    }
}