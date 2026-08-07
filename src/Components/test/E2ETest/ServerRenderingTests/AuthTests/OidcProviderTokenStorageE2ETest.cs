// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.AuthTests;

public class OidcProviderTokenStorageE2ETest
    : ServerTestBase<TrimmingServerFixture<RemoteAuthenticationTokenStorageStartup>>
{
    private const string OidcUserKeyPrefix = "oidc.user:";

    public OidcProviderTokenStorageE2ETest(
        BrowserFixture browserFixture,
        TrimmingServerFixture<RemoteAuthenticationTokenStorageStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void TokenStorage_LocalStorage_PageDisplaysLocalStorage_AndTokensAreStoredInLocalStorage()
    {
        Navigate($"{RemoteAuthenticationTokenStorageStartup.TokenStorageBasePath}/test-token-storage");

        ClearOidcUserFromStorages();
        Browser.Navigate().Refresh();

        AssertSignedInAsJaneDoe();
        AssertPageDisplaysStorage("LocalStorage");

        AssertOidcUserIsStoredIn(expectedStorage: "localStorage", notExpectedStorage: "sessionStorage");
    }

    [Fact]
    public void TokenStorage_SessionStorage_PageDisplaysSessionStorage_AndTokensAreStoredInSessionStorage()
    {
        Navigate($"{RemoteAuthenticationTokenStorageStartup.SessionStorageBasePath}/test-session-storage");

        ClearOidcUserFromStorages();
        Browser.Navigate().Refresh();

        AssertSignedInAsJaneDoe();
        AssertPageDisplaysStorage("SessionStorage");

        AssertOidcUserIsStoredIn(expectedStorage: "sessionStorage", notExpectedStorage: "localStorage");
    }

    [Fact]
    public void OidcProviderOptions_TokenStorage_RoundTripsThroughJson()
    {
        var options = new OidcProviderOptions
        {
            TokenStorage = RemoteAuthenticationTokenStorage.LocalStorage,
        };

        var json = JsonSerializer.Serialize(options);
        var roundTripped = JsonSerializer.Deserialize<OidcProviderOptions>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(
            RemoteAuthenticationTokenStorage.LocalStorage,
            roundTripped!.TokenStorage);
    }

    private void AssertSignedInAsJaneDoe()
    {
        var heading = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello, Jane Doe!", () => heading.Text);
    }

    private void AssertPageDisplaysStorage(string expectedStorageDisplay)
    {
        var storageElement = Browser.Exists(By.Id("token-storage-type"));
        Browser.Equal(expectedStorageDisplay, () => storageElement.Text);
    }

    private void AssertOidcUserIsStoredIn(string expectedStorage, string notExpectedStorage)
    {
        var expectedHasUser = CountKeysWithPrefix(expectedStorage);
        var otherHasUser = CountKeysWithPrefix(notExpectedStorage);

        Assert.True(
            expectedHasUser > 0,
            $"Expected at least one '{OidcUserKeyPrefix}*' key in {expectedStorage}, but found {expectedHasUser}.");
        Assert.True(
            otherHasUser == 0,
            $"Did not expect any '{OidcUserKeyPrefix}*' key in {notExpectedStorage}, but found {otherHasUser}.");
    }

    private void ClearOidcUserFromStorages()
    {
        ClearKeysWithPrefix("localStorage");
        ClearKeysWithPrefix("sessionStorage");
    }

    private void ClearKeysWithPrefix(string storageName)
    {
        ExecuteStorageScript(storageName, clear: true);
    }

    private long CountKeysWithPrefix(string storageName)
    {
        return ExecuteStorageScript(storageName, clear: false);
    }

    private long ExecuteStorageScript(string storageName, bool clear)
    {
        var js = (IJavaScriptExecutor)Browser;
        var script =
            $@"return (function() {{
                var store = window[{ToJsString(storageName)}];
                if (!store) {{ return -1; }}
                var keys = [];
                for (var i = 0; i < store.length; i++) {{
                    var k = store.key(i);
                    if (k && k.indexOf({ToJsString(OidcUserKeyPrefix)}) === 0) {{
                        keys.push(k);
                    }}
                }}
                if ({(clear ? "true" : "false")}) {{
                    for (var j = 0; j < keys.length; j++) {{
                        store.removeItem(keys[j]);
                    }}
                }}
                return keys.length;
            }})();";

        var result = js.ExecuteScript(script);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static string ToJsString(string value) =>
        JsonSerializer.Serialize(value);
}