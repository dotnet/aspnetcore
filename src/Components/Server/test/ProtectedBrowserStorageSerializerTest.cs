// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class ProtectedBrowserStorageSerializerTest
{
    [Fact]
    public async Task SetAsync_UsesTheRegisteredSerializer()
    {
        var jsRuntime = new CapturingJSRuntime();
        var storage = CreateStorage(jsRuntime, new ThemeSerializer());

        await storage.SetAsync("theme", new Theme("dark"));

        Assert.Equal("dark", Unprotect(jsRuntime.LastSetValue));
    }

    [Fact]
    public async Task GetAsync_UsesTheRegisteredSerializer()
    {
        var jsRuntime = new CapturingJSRuntime { NextGetValue = Protect("dark") };
        var storage = CreateStorage(jsRuntime, new ThemeSerializer());

        var result = await storage.GetAsync<Theme>("theme");

        Assert.True(result.Success);
        Assert.Equal("dark", result.Value!.Name);
    }

    [Fact]
    public async Task SetAsync_RoundTripsThroughTheRegisteredSerializer()
    {
        var jsRuntime = new CapturingJSRuntime();
        var storage = CreateStorage(jsRuntime, new ThemeSerializer());

        await storage.SetAsync("theme", new Theme("solarized"));
        jsRuntime.NextGetValue = jsRuntime.LastSetValue;

        var result = await storage.GetAsync<Theme>("theme");

        Assert.Equal("solarized", result.Value!.Name);
    }

    [Fact]
    public async Task SetAsync_UsesTheRegisteredSerializerWithAnExplicitPurpose()
    {
        var jsRuntime = new CapturingJSRuntime();
        var storage = CreateStorage(jsRuntime, new ThemeSerializer());

        await storage.SetAsync("custom-purpose", "theme", new Theme("dark"));

        Assert.Equal("dark", Unprotect(jsRuntime.LastSetValue));
    }

    [Fact]
    public async Task SetAsync_SerializesAsJsonWhenNoSerializerIsRegistered()
    {
        var jsRuntime = new CapturingJSRuntime();
        var storage = CreateStorage(jsRuntime, serializer: null);

        await storage.SetAsync("theme", new Theme("dark"));

        Assert.Equal("{\"name\":\"dark\"}", Unprotect(jsRuntime.LastSetValue));
    }

    [Fact]
    public async Task SetAsync_SerializesAsJsonWhenTheValueIsStaticallyTypedAsObject()
    {
        var jsRuntime = new CapturingJSRuntime();
        var storage = CreateStorage(jsRuntime, new ThemeSerializer());
        object value = new Theme("dark");

        // The non-generic overload wins when the argument is statically typed as object, which is what
        // keeps existing call sites on the JSON path now that the generic overloads exist.
        await storage.SetAsync("theme", value);

        Assert.Equal("{\"name\":\"dark\"}", Unprotect(jsRuntime.LastSetValue));
    }

    [Fact]
    public async Task SetAsync_OnlyUsesTheSerializerRegisteredForThatType()
    {
        var jsRuntime = new CapturingJSRuntime();
        var storage = CreateStorage(jsRuntime, new ThemeSerializer());

        await storage.SetAsync("count", 42);

        Assert.Equal("42", Unprotect(jsRuntime.LastSetValue));
    }

    [Fact]
    public async Task SetAsync_JsonFallbackPreservesRuntimeType()
    {
        var jsRuntime = new CapturingJSRuntime();
        var storage = CreateStorage(jsRuntime, serializer: null);
        BaseTheme value = new DerivedTheme("dark", "high");

        await storage.SetAsync("theme", value);

        Assert.Equal("{\"contrast\":\"high\",\"name\":\"dark\"}", Unprotect(jsRuntime.LastSetValue));
    }

    private static ProtectedLocalStorage CreateStorage(IJSRuntime jsRuntime, ProtectedBrowserStorageSerializer<Theme>? serializer)
    {
        var services = new ServiceCollection();
        if (serializer is not null)
        {
            services.AddSingleton(serializer);
        }

        return new ProtectedLocalStorage(
            jsRuntime,
            new PassthroughDataProtectionProvider(),
            new JsonSerializerOptions(JsonSerializerOptions.Web),
            services.BuildServiceProvider());
    }

    private static string Protect(string value)
        => new PassthroughDataProtectionProvider().CreateProtector("ignored").Protect(value);

    private static string Unprotect(string? protectedValue)
        => new PassthroughDataProtectionProvider().CreateProtector("ignored").Unprotect(protectedValue!);

    private sealed class Theme(string name)
    {
        public string Name { get; set; } = name;
    }

    private class BaseTheme(string name)
    {
        public string Name { get; set; } = name;
    }

    private sealed class DerivedTheme(string name, string contrast) : BaseTheme(name)
    {
        public string Contrast { get; set; } = contrast;
    }

    private sealed class ThemeSerializer : ProtectedBrowserStorageSerializer<Theme>
    {
        public override string Serialize(Theme value) => value.Name;

        public override Theme Deserialize(string data) => new(data);
    }

    private sealed class CapturingJSRuntime : IJSRuntime
    {
        public string? LastSetValue { get; private set; }

        public string? NextGetValue { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier.EndsWith("setItem", StringComparison.Ordinal))
            {
                LastSetValue = (string?)args![1];
                return default;
            }

            return (ValueTask<TValue>)(object)ValueTask.FromResult(NextGetValue);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    // Data protection is not what these tests are about, so the payload is left intact and only the
    // base64url wrapping that Protect(string) applies is exercised.
    private sealed class PassthroughDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose) => new PassthroughDataProtector();

        private sealed class PassthroughDataProtector : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => this;

            public byte[] Protect(byte[] plaintext) => plaintext;

            public byte[] Unprotect(byte[] protectedData) => protectedData;
        }
    }
}
