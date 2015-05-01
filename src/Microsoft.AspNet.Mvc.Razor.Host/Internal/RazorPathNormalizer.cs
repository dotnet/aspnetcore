// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Internal
{
    public class RazorPathNormalizer
    {
        public virtual string NormalizePath([NotNull] string path)
        {
            return path;
        }
    }
}