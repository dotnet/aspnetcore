// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

public abstract class ServerTestBase<TServerFixture>
    : BrowserAdapterTestBase,
    IClassFixture<TServerFixture>
    where TServerFixture : ServerFixture
{
    public string ServerPathBase => "/subdir";

    protected readonly TServerFixture _serverFixture;

    public ServerTestBase(
        BrowserFixture browserFixture,
        TServerFixture serverFixture,
        ITestOutputHelper output)
        : base(output)
    {
        _serverFixture = serverFixture;
    }

    public void Navigate(string relativeUrl, bool noReload = false)
    {
        Browser.Navigate(_serverFixture.RootUri, relativeUrl, noReload);
    }
}
