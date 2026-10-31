// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestSinkDirectory : ManifestDirectory
{
    private ManifestSinkDirectory()
        : base(name: string.Empty, children: Array.Empty<ManifestEntry>())
    {
        SetParent(this);
        Children = new[] { this };
    }

    public static ManifestDirectory Instance { get; } = new ManifestSinkDirectory();

    public override ManifestEntry Traverse(StringSegment segment) => this;
}
