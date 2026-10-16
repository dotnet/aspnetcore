// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformTestApp.Components;
using PlatformTestApp.E2E.Tests.Fixtures;

namespace PlatformTestApp.E2E.Tests.Tests;

[UITest]
public partial class CookieStoreTests : BrowserTest
{
    private static readonly string[] CookieStorePaths = ["/cookiestore-server", "/cookiestore-wasm"];

    private ServerInstance _server = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        _context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await _context.NewPageAsync();
    }

    [TestMethod]
    public async Task CookieStore_SetsGetsAndDeletesCookiesInBothRenderModes()
    {
        foreach (var path in CookieStorePaths)
        {
            await NavigateAsync(path, "#set-cookie");

            var name = path == "/cookiestore-server" ? "server-cookie" : "wasm-cookie";
            await _page.Locator("#cookie-name").FillAsync(name);
            await _page.Locator("#cookie-value").FillAsync("value");
            await RunAsync("#set-cookie", $"Set: {name}");
            AssertDefaultAttributes(await GetBrowserCookieAsync(name));

            await RunAsync("#get-cookie", $"Found: {name}=value");
            await RunAsync("#get-cookie-with-options", $"Found: {name}=value");
            await RunAsync("#get-all-cookies", $"Found: {name}=value");
            await RunAsync("#get-all-cookies-with-name", $"Found: {name}=value");
            await RunAsync("#get-all-cookies-with-options", $"Found: {name}=value");

            await RunAsync("#delete-cookie", $"Deleted: {name}");
            await RunAsync("#get-cookie", "Cookie is missing.");
            await RunAsync("#get-all-cookies", "No cookies.");
        }
    }

    [TestMethod]
    public async Task CookieStore_GetsAllOfSeveralCookies()
    {
        foreach (var path in CookieStorePaths)
        {
            await NavigateAsync(path, "#set-cookie");

            var prefix = path == "/cookiestore-server" ? "server" : "wasm";
            await _page.Locator("#cookie-value").FillAsync("value");
            foreach (var name in new[] { $"{prefix}-first", $"{prefix}-second" })
            {
                await _page.Locator("#cookie-name").FillAsync(name);
                await RunAsync("#set-cookie", $"Set: {name}");
            }

            // Cookies with the same path come back in the order of creation.
            await RunAsync("#get-all-cookies", $"Found: {prefix}-first=value; {prefix}-second=value");

            foreach (var name in new[] { $"{prefix}-first", $"{prefix}-second" })
            {
                await _page.Locator("#cookie-name").FillAsync(name);
                await RunAsync("#delete-cookie", $"Deleted: {name}");
            }
        }
    }

    [TestMethod]
    public async Task CookieStore_SendsEveryCookieOptionToTheBrowser()
    {
        foreach (var path in CookieStorePaths)
        {
            await NavigateAsync(path, "#set-cookie");

            var name = path == "/cookiestore-server" ? "server-options" : "wasm-options";
            await _page.Locator("#cookie-name").FillAsync(name);

            await RunAsync("#set-cookie-with-defaults", $"Set with defaults: {name}");
            AssertDefaultAttributes(await GetBrowserCookieAsync(name));
            await RunAsync("#delete-cookie", $"Deleted: {name}");

            await RunAsync("#set-cookie-with-options", $"Set with options: {name}");
            var cookie = await GetBrowserCookieAsync(name);
            // The browser adds a trailing slash to the path.
            Assert.AreEqual("/cookie-path/", cookie.Path);
            Assert.AreEqual(SameSiteAttribute.Lax, cookie.SameSite);
            AssertExpiresIn(cookie, TimeSpan.FromDays(1));

            await RunAsync("#delete-cookie-with-options", $"Deleted with options: {name}");
            Assert.DoesNotContain(name, (await _context.CookiesAsync()).Select(browserCookie => browserCookie.Name));

            await RunAsync("#set-cookie-with-max-age", $"Set with max age: {name}");
            cookie = await GetBrowserCookieAsync(name);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
            AssertExpiresIn(cookie, TimeSpan.FromHours(1));
            await RunAsync("#delete-cookie", $"Deleted: {name}");

            await RunAsync("#set-cookie-with-other-domain", "Another domain was rejected.");
            Assert.DoesNotContain(name, (await _context.CookiesAsync()).Select(browserCookie => browserCookie.Name));
        }
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }

    private async Task RunAsync(string buttonSelector, string expectedResult)
    {
        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(_page.Locator("#cookie-result")).ToHaveTextAsync(expectedResult);
    }

    private async Task<BrowserContextCookiesResult> GetBrowserCookieAsync(string name)
    {
        var cookies = await _context.CookiesAsync();

        return cookies.Single(cookie => cookie.Name == name);
    }

    private static void AssertDefaultAttributes(BrowserContextCookiesResult cookie)
    {
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(SameSiteAttribute.Strict, cookie.SameSite);
        // Playwright reports a session cookie with -1.
        Assert.AreEqual(-1, cookie.Expires);
    }

    private static void AssertExpiresIn(BrowserContextCookiesResult cookie, TimeSpan expected)
    {
        var actual = DateTimeOffset.FromUnixTimeMilliseconds((long)(cookie.Expires * 1000)) - DateTimeOffset.UtcNow;

        Assert.IsLessThan(
            TimeSpan.FromMinutes(5),
            (actual - expected).Duration(),
            $"The cookie expires in {actual}, not in {expected}.");
    }
}
