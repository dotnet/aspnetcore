// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.DataProtection.Internal;

internal class HostingApplicationDiscriminator : IApplicationDiscriminator
{
    private readonly IHostEnvironment? _hosting;

    // the optional constructor for when IHostingEnvironment is not available from DI
    public HostingApplicationDiscriminator()
    {
    }

    public HostingApplicationDiscriminator(IHostEnvironment hosting)
    {
        _hosting = hosting;
    }

    // Note: ContentRootPath behavior depends on the version, sometimes it has a trailing slash, we normalize by default by removing a trailing slash
    public string? Discriminator => _hosting?.ContentRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
