// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Playwright;

/// <summary>
/// Base class for MSTest tests that need a Playwright <see cref="IBrowserContext"/> per
/// test. Mirrors the shape of <c>Microsoft.Playwright.MSTest.ContextTest</c>.
/// </summary>
/// <remarks>
/// A fresh <see cref="Context"/> is created in <c>[TestInitialize]</c> using the options
/// returned by <see cref="GetContextOptions"/> and is automatically disposed at end of
/// test via the <see cref="BrowserTest"/> base.
/// </remarks>
[TestClass]
public abstract class ContextTest : BrowserTest
{
    /// <summary>The browser context for the current test.</summary>
    public IBrowserContext Context { get; private set; } = null!;

    /// <summary>
    /// Override to customize the <see cref="BrowserNewContextOptions"/> used to create
    /// <see cref="Context"/>. Default returns an empty options object.
    /// </summary>
    public virtual BrowserNewContextOptions GetContextOptions() => new();

    /// <summary>MSTest hook: creates the per-test <see cref="Context"/>.</summary>
    [TestInitialize]
    public async Task ContextTestSetup()
    {
        Context = await NewContext(GetContextOptions()).ConfigureAwait(false);
    }
}
