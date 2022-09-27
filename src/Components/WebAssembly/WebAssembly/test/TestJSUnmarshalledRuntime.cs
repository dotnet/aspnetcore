// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal class TestJSUnmarshalledRuntime : IJSUnmarshalledRuntime
{
    private readonly string _environment;

    public TestJSUnmarshalledRuntime(string environment = "Production")
    {
        _environment = environment;
    }

    public TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
    {
        switch (identifier)
        {
            case "Blazor._internal.getApplicationEnvironment":
                return (TResult)(object)_environment;
            case "Blazor._internal.getConfig":
                return (TResult)(object)null;
            case "Blazor._internal.navigationManager.getUnmarshalledBaseURI":
                var testUri = "https://www.example.com/awesome-part-that-will-be-truncated-in-tests";
                return (TResult)(object)testUri;
            case "Blazor._internal.navigationManager.getUnmarshalledLocationHref":
                var testHref = "https://www.example.com/awesome-part-that-will-be-truncated-in-tests/cool";
                return (TResult)(object)testHref;
            case "Blazor._internal.registeredComponents.getRegisteredComponentsCount":
                return (TResult)(object)0;
            case "Blazor._internal.getPersistedState":
                return (TResult)(object)null;
            default:
                throw new NotImplementedException($"{nameof(TestJSUnmarshalledRuntime)} has no implementation for '{identifier}'.");
        }
    }

    public TResult InvokeUnmarshalled<TResult>(string identifier)
        => InvokeUnmarshalled<object, object, object, TResult>(identifier, null, null, null);

    public TResult InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0)
        => InvokeUnmarshalled<T0, object, object, TResult>(identifier, arg0, null, null);

    public TResult InvokeUnmarshalled<T0, T1, TResult>(string identifier, T0 arg0, T1 arg1)
        => InvokeUnmarshalled<T0, T1, object, TResult>(identifier, arg0, arg1, null);
}
