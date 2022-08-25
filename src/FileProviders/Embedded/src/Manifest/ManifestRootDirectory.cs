// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestRootDirectory : ManifestDirectory
{
    public ManifestRootDirectory(ManifestEntry[] children)
        : base(name: string.Empty, children: children)
    {
        SetParent(ManifestSinkDirectory.Instance);
    }

    public override ManifestDirectory ToRootDirectory() => this;
}
