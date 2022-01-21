// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupWithServices
{
    private readonly IFakeStartupCallback _fakeStartupCallback;

    public StartupWithServices(IFakeStartupCallback fakeStartupCallback)
    {
        _fakeStartupCallback = fakeStartupCallback;
    }

    public void Configure(IApplicationBuilder builder, IFakeStartupCallback fakeStartupCallback2)
    {
        _fakeStartupCallback.ConfigurationMethodCalled(this);
        fakeStartupCallback2.ConfigurationMethodCalled(this);
    }
}
