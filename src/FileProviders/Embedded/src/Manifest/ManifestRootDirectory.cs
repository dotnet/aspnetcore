// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    internal class ManifestRootDirectory : ManifestDirectory
    {
        public ManifestRootDirectory(ManifestEntry[] children)
            : base(name: null, children: children)
        {
            SetParent(ManifestSinkDirectory.Instance);
        }

        public override ManifestDirectory ToRootDirectory() => this;
    }
}
