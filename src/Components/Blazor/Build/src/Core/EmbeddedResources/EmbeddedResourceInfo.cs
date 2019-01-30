// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Build
{
    internal class EmbeddedResourceInfo
    {
        public EmbeddedResourceKind Kind { get; }
        public string RelativePath { get; }

        public EmbeddedResourceInfo(EmbeddedResourceKind kind, string relativePath)
        {
            Kind = kind;
            RelativePath = relativePath;
        }
    }
}
