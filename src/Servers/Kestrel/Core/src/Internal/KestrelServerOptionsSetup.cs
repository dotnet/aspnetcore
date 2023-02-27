// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class KestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
{
    private readonly IServiceProvider _services;
    private readonly bool _disableDefaultCertificate;

    public KestrelServerOptionsSetup(IServiceProvider services)
    {
        _services = services;
        _disableDefaultCertificate = true;
    }

    public KestrelServerOptionsSetup(IServiceProvider services, ITlsConfigurationLoader _)
    {
        _services = services;
    }

    public void Configure(KestrelServerOptions options)
    {
        options.ApplicationServices = _services;
        options.DisableDefaultCertificate = _disableDefaultCertificate;
    }
}
