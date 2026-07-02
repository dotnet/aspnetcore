// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Playwright;

/// <summary>
/// Base class for MSTest tests that need access to a shared <see cref="IPlaywright"/>
/// instance and the static <see cref="Expect(IPage)"/> assertion helpers. Mirrors the
/// shape of <c>Microsoft.Playwright.MSTest.PlaywrightTest</c> without taking a
/// dependency on that package.
/// </summary>
/// <remarks>
/// The <see cref="IPlaywright"/> instance is created lazily on first access and is
/// shared by every test in the test assembly. It is intentionally not disposed —
/// the underlying Node.js driver process is reaped when the test host exits.
/// </remarks>
[TestClass]
public abstract class PlaywrightTest
{
    private static IPlaywright? s_playwright;
    private static readonly SemaphoreSlim s_initLock = new(1, 1);

    /// <summary>The MSTest test context for the current test.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>The shared Playwright instance. Initialized on first use.</summary>
    public IPlaywright Playwright =>
        s_playwright ?? throw new InvalidOperationException(
            $"Playwright has not been initialized. Call {nameof(EnsurePlaywrightAsync)} from a test " +
            "initializer or use a derived class that does so (e.g. BrowserTest, ContextTest, PageTest).");

    /// <summary>
    /// Returns the shared <see cref="IPlaywright"/>, creating it on first call. Safe to
    /// invoke concurrently — initialization is serialized.
    /// </summary>
    public static async Task<IPlaywright> EnsurePlaywrightAsync()
    {
        if (s_playwright is not null)
        {
            return s_playwright;
        }

        await s_initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            s_playwright ??= await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
            return s_playwright;
        }
        finally
        {
            s_initLock.Release();
        }
    }

    /// <summary>Returns assertions on the given <see cref="IPage"/>.</summary>
    public static IPageAssertions Expect(IPage page) => Assertions.Expect(page);

    /// <summary>Returns assertions on the given <see cref="ILocator"/>.</summary>
    public static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    /// <summary>Returns assertions on the given <see cref="IAPIResponse"/>.</summary>
    public static IAPIResponseAssertions Expect(IAPIResponse response) => Assertions.Expect(response);
}
