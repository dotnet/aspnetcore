// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestFile : ManifestEntry
{
    public ManifestFile(string name, string resourcePath)
        : base(name)
    {
        ArgumentThrowHelper.ThrowIfNullOrWhiteSpace(name);
        ArgumentThrowHelper.ThrowIfNullOrWhiteSpace(resourcePath);

        ResourcePath = resourcePath;
    }

    public string ResourcePath { get; }

    public override ManifestEntry Traverse(StringSegment segment) => UnknownPath;
}
