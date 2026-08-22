// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components;

public class ElementReferenceExtensionsTest
{
    [Fact]
    public async Task BlurAsync_InvokesDomWrapperBlur()
    {
        var jsRuntime = new TestJSRuntime();
        var elementReference = new ElementReference("element-id", new WebElementReferenceContext(jsRuntime));

        await elementReference.BlurAsync();

        Assert.Collection(
            jsRuntime.Invocations,
            invocation =>
            {
                Assert.Equal("Blazor._internal.domWrapper.blur", invocation.Identifier);
                var args = invocation.Args.Cast<object?>().ToArray();
                Assert.Single(args);
                var passedRef = Assert.IsType<ElementReference>(args[0]);
                Assert.Equal(elementReference.Id, passedRef.Id);
            });
    }

    [Fact]
    public async Task BlurAsync_PropagatesJSRuntimeException()
    {
        var jsRuntime = new TestJSRuntime
        {
            NextInvocationException = new InvalidOperationException("blur failed")
        };
        var elementReference = new ElementReference("element-id", new WebElementReferenceContext(jsRuntime));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => elementReference.BlurAsync().AsTask());
        Assert.Equal("blur failed", ex.Message);
    }

    [Fact]
    public void BlurAsync_ThrowsWhenContextIsNotWebElementReferenceContext()
    {
        var elementReference = new ElementReference("id", new NonWebElementReferenceContext());
        var ex = Assert.Throws<InvalidOperationException>(() => elementReference.BlurAsync());
        Assert.Equal("ElementReference has not been configured correctly.", ex.Message);
    }

    [Fact]
    public void BlurAsync_ThrowsWhenNoJSRuntime()
    {
        var elementReference = default(ElementReference);
        Assert.Throws<InvalidOperationException>(() => elementReference.BlurAsync());
    }

    private sealed class TestJSRuntime : IJSRuntime
    {
        public List<(string Identifier, IReadOnlyList<object?> Args)> Invocations { get; } = new();

        public Exception? NextInvocationException { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Record(identifier, args);
            if (NextInvocationException is { } ex)
            {
                NextInvocationException = null;
                throw ex;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }

        private void Record(string identifier, object?[]? args)
        {
            Invocations.Add((identifier, (IReadOnlyList<object?>)(args ?? Array.Empty<object?>())));
        }
    }

    private sealed class NonWebElementReferenceContext : ElementReferenceContext
    {
    }
}
