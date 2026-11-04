// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.DataProtection.Internal;

internal sealed class HostingApplicationDiscriminator : IApplicationDiscriminator
{
    private readonly IHostEnvironment? _hosting;
    private readonly string DirectorySeparator = Path.DirectorySeparatorChar.ToString();
    private readonly string AltDirectorySeparator = Path.AltDirectorySeparatorChar.ToString();

    // the optional constructor for when IHostingEnvironment is not available from DI
    public HostingApplicationDiscriminator()
    {
    }

    public HostingApplicationDiscriminator(IHostEnvironment hosting)
    {
        _hosting = hosting;
    }

    // Note: ContentRootPath behavior depends on the version, sometimes it does not have a trailing slash,
    // we normalize by adding a trailing slash for non whitespace content root paths so data protection
    // works across versions
    public string? Discriminator
    {
        get
        {
            var contentRoot = _hosting?.ContentRootPath?.Trim();
            if (string.IsNullOrEmpty(contentRoot) ||
                contentRoot.EndsWith(DirectorySeparator, StringComparison.OrdinalIgnoreCase) ||
                contentRoot.EndsWith(AltDirectorySeparator, StringComparison.OrdinalIgnoreCase))
            {
                return contentRoot;
            }
            return contentRoot + DirectorySeparator;
        }
    }
}
