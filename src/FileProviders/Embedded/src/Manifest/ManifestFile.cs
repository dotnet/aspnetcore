// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestFile : ManifestEntry
{
    public ManifestFile(string name, string resourcePath)
        : base(name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' must not be null, empty or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            throw new ArgumentException($"'{nameof(resourcePath)}' must not be null, empty or whitespace.", nameof(resourcePath));
        }

        ResourcePath = resourcePath;
    }

    public string ResourcePath { get; }

    public override ManifestEntry Traverse(StringSegment segment) => UnknownPath;
}
