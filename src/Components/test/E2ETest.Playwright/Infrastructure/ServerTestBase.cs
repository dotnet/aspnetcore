// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

public abstract class ServerTestBase<TServerFixture>
    : BrowserTestBase,
    IClassFixture<TServerFixture>
    where TServerFixture : ServerFixture
{
    public string ServerPathBase => "/subdir";

    protected readonly TServerFixture _serverFixture;

    public ServerTestBase(
        TServerFixture serverFixture,
        ITestOutputHelper output)
        : base(output)
    {
        _serverFixture = serverFixture;
    }

    public void Navigate(string relativeUrl, bool noReload = false)
    {
        //Browser.Navigate(_serverFixture.RootUri, relativeUrl, noReload);
    }
}
