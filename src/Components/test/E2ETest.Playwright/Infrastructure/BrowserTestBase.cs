// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;

namespace Microsoft.AspNetCore.E2ETesting;

public class BrowserTestBase : IClassFixture<BrowserFixture>
{
    private readonly BrowserFixture _browserFixture;

    public BrowserTestBase(BrowserFixture browserFixture, ITestOutputHelper output)
    {
        _browserFixture = browserFixture;
    }
}

public class BrowserFixture
{
}
