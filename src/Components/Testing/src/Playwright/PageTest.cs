// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Playwright;

/// <summary>
/// Base class for MSTest tests that need a Playwright <see cref="IPage"/> per test.
/// Mirrors the shape of <c>Microsoft.Playwright.MSTest.PageTest</c>.
/// </summary>
/// <remarks>
/// A fresh <see cref="Page"/> is created in <c>[TestInitialize]</c> on top of the
/// <see cref="ContextTest.Context"/> and is closed when its parent context is disposed
/// at end of test.
/// </remarks>
[TestClass]
public abstract class PageTest : ContextTest
{
    /// <summary>The page for the current test.</summary>
    public IPage Page { get; private set; } = null!;

    /// <summary>MSTest hook: creates the per-test <see cref="Page"/>.</summary>
    [TestInitialize]
    public async Task PageTestSetup()
    {
        Page = await Context.NewPageAsync().ConfigureAwait(false);
    }
}
