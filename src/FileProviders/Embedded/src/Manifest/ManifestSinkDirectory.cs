// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    internal class ManifestSinkDirectory : ManifestDirectory
    {
        private ManifestSinkDirectory()
            : base(name: null, children: Array.Empty<ManifestEntry>())
        {
            SetParent(this);
            Children = new[] { this };
        }

        public static ManifestDirectory Instance { get; } = new ManifestSinkDirectory();

        public override ManifestEntry Traverse(StringSegment segment) => this;
    }
}
