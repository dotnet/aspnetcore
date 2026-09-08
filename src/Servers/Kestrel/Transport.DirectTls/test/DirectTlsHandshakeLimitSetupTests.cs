// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class DirectTlsHandshakeLimitSetupTests
{
    [Fact]
    public void PostConfigure_DefaultsFromMaxConcurrentConnections_WhenUnset()
    {
        var serverOptions = new KestrelServerOptions();
        serverOptions.Limits.MaxConcurrentConnections = 1234;
        var setup = new DirectTlsHandshakeLimitSetup(Options.Create(serverOptions));
        var transportOptions = new DirectTlsTransportOptions();

        setup.PostConfigure(name: null, transportOptions);

        Assert.Equal(1234, transportOptions.MaxConcurrentHandshakes);
    }

    [Fact]
    public void PostConfigure_LeavesExplicitValueUntouched()
    {
        var serverOptions = new KestrelServerOptions();
        serverOptions.Limits.MaxConcurrentConnections = 1234;
        var setup = new DirectTlsHandshakeLimitSetup(Options.Create(serverOptions));
        var transportOptions = new DirectTlsTransportOptions { MaxConcurrentHandshakes = 7 };

        setup.PostConfigure(name: null, transportOptions);

        Assert.Equal(7, transportOptions.MaxConcurrentHandshakes);
    }

    [Fact]
    public void PostConfigure_StaysUnlimited_WhenConnectionLimitUnset()
    {
        var serverOptions = new KestrelServerOptions();
        Assert.Null(serverOptions.Limits.MaxConcurrentConnections);
        var setup = new DirectTlsHandshakeLimitSetup(Options.Create(serverOptions));
        var transportOptions = new DirectTlsTransportOptions();

        setup.PostConfigure(name: null, transportOptions);

        Assert.Null(transportOptions.MaxConcurrentHandshakes);
    }
}
