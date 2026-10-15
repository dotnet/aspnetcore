// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class JSCallbackTest
{
    [Fact]
    public async Task CreateAsync_ImportsTheModuleOnceAndPassesTheShapes()
    {
        var jsRuntime = new BrowserJSRuntime();
        var factory = new JSCallbackFactory(jsRuntime);

        await factory.CreateAsync(new JSCallback<string>(), "[null]");
        await factory.CreateAsync(new JSCallback<string, int>(), "[null,{}]");

        Assert.Equal(
            ["""["./_content/Microsoft.AspNetCore.Components.Platform/Microsoft.AspNetCore.Components.Platform.js"]"""],
            jsRuntime.CallsTo("import").Select(call => call.ArgsJson));
        Assert.Equal(
            ["""[{"__dotNetObject":1},"[null]"]""", """[{"__dotNetObject":2},"[null,{}]"]"""],
            jsRuntime.CallsTo("createCallback").Select(call => call.ArgsJson));
    }

    [Fact]
    public async Task Invoke_PassesEachArgumentToTheHandler()
    {
        var jsRuntime = new BrowserJSRuntime();
        var factory = new JSCallbackFactory(jsRuntime);
        var callback = new JSCallback<string, int, CookieListItem>();
        (string, int, CookieListItem)? received = null;
        callback.Handler = (text, number, cookie) =>
        {
            received = (text, number, cookie);
            return Task.CompletedTask;
        };
        await factory.CreateAsync(callback, "[null,null,null]");

        var result = await jsRuntime.InvokeLastCallbackAsync("""["text",42,{"name":"a","value":"1"}]""");

        Assert.True(result.Success, result.Exception?.ToString());
        Assert.Equal("text", received?.Item1);
        Assert.Equal(42, received?.Item2);
        Assert.Equal(("a", "1"), (received?.Item3.Name, received?.Item3.Value));
    }

    [Fact]
    public async Task Invoke_WaitsForTheHandler()
    {
        var jsRuntime = new BrowserJSRuntime();
        var factory = new JSCallbackFactory(jsRuntime);
        var handlerCompletion = new TaskCompletionSource();
        var callback = new JSCallback<string> { Handler = _ => handlerCompletion.Task };
        await factory.CreateAsync(callback, "[null]");

        var invocation = jsRuntime.InvokeLastCallbackAsync("""["text"]""");
        Assert.False(invocation.IsCompleted);
        handlerCompletion.SetResult();

        Assert.True((await invocation).Success);
    }

    [Fact]
    public async Task Invoke_SucceedsWithoutAHandler()
    {
        var jsRuntime = new BrowserJSRuntime();
        var factory = new JSCallbackFactory(jsRuntime);
        await factory.CreateAsync(new JSCallback<string>(), "[null]");

        var result = await jsRuntime.InvokeLastCallbackAsync("""["text"]""");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Disposal_ReleasesFunctionsTargetsAndModule()
    {
        var jsRuntime = new BrowserJSRuntime();
        var factory = new JSCallbackFactory(jsRuntime);
        var callback = new JSCallback<string> { Handler = _ => Task.CompletedTask };
        await factory.CreateAsync(callback, "[null]");

        await ((IInternalAsyncDisposal)factory).InternalDisposeAsync();
        var result = await jsRuntime.InvokeLastCallbackAsync("""["text"]""");

        // The module is object 1 and the function is object 2.
        Assert.Equal(["[2]", "[1]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(call => call.ArgsJson));
        Assert.False(result.Success);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => factory.CreateAsync(callback, "[null]"));
    }
}
