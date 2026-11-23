// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Microsoft.JSInterop;

public class JSRuntimeExtensionsTest
{
    [Fact]
    public async Task InvokeAsync_WithParamsArgs()
    {
        // Arrange
        var method = "someMethod";
        var expected = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<string>(method, It.IsAny<object[]>()))
            .Callback<string, object[]>((method, args) =>
            {
                Assert.Equal(expected, args);
            })
            .Returns(new ValueTask<string>("Hello"))
            .Verifiable();

        // Act
        var result = await jsRuntime.Object.InvokeAsync<string>(method, "a", "b");

        // Assert
        Assert.Equal("Hello", result);
        jsRuntime.Verify();
    }

    [Fact]
    public async Task InvokeAsync_WithParamsArgsAndCancellationToken()
    {
        // Arrange
        var method = "someMethod";
        var expected = new[] { "a", "b" };
        var cancellationToken = new CancellationToken();
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<string>(method, cancellationToken, It.IsAny<object[]>()))
            .Callback<string, CancellationToken, object[]>((method, cts, args) =>
            {
                Assert.Equal(expected, args);
            })
            .Returns(new ValueTask<string>("Hello"))
            .Verifiable();

        // Act
        var result = await jsRuntime.Object.InvokeAsync<string>(method, cancellationToken, "a", "b");

        // Assert
        Assert.Equal("Hello", result);
        jsRuntime.Verify();
    }

    [Fact]
    public async Task InvokeVoidAsync_WithoutCancellationToken()
    {
        // Arrange
        var method = "someMethod";
        var args = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<IJSVoidResult>(method, args)).Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));

        // Act
        await jsRuntime.Object.InvokeVoidAsync(method, args);

        jsRuntime.Verify();
    }

    [Fact]
    public async Task InvokeVoidAsync_WithCancellationToken()
    {
        // Arrange
        var method = "someMethod";
        var args = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<IJSVoidResult>(method, It.IsAny<CancellationToken>(), args)).Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));

        // Act
        await jsRuntime.Object.InvokeVoidAsync(method, new CancellationToken(), args);

        jsRuntime.Verify();
    }

    [Fact]
    public async Task InvokeAsync_WithTimeout()
    {
        // Arrange
        var expected = "Hello";
        var method = "someMethod";
        var args = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<string>(method, It.IsAny<CancellationToken>(), args))
            .Callback<string, CancellationToken, object[]>((method, cts, args) =>
            {
                // There isn't a very good way to test when the cts will cancel. We'll just verify that
                // it'll get cancelled eventually.
                Assert.True(cts.CanBeCanceled);
            })
            .Returns(new ValueTask<string>(expected));

        // Act
        var result = await jsRuntime.Object.InvokeAsync<string>(method, TimeSpan.FromMinutes(5), args);

        Assert.Equal(expected, result);
        jsRuntime.Verify();
    }

    [Fact]
    public async Task InvokeAsync_WithTimeout_DoesNotWrapUnrelatedCancellation()
    {
        using var otherCancellation = new CancellationTokenSource();
        otherCancellation.Cancel();
        var completion = new TaskCompletionSource<string>();
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<string>("someMethod", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .Returns((string identifier, CancellationToken token, object[] args) =>
            {
                token.Register(() => completion.TrySetCanceled(otherCancellation.Token));
                return new ValueTask<string>(completion.Task);
            });

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await jsRuntime.Object.InvokeAsync<string>("someMethod", TimeSpan.FromMilliseconds(100), Array.Empty<object>()));

        Assert.Equal(otherCancellation.Token, exception.CancellationToken);
    }

    [Theory]
    [InlineData("Get")]
    [InlineData("Set")]
    public void Timeout_WrapsSynchronousCancellation(string operation)
    {
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.GetValueAsync<object>("someProperty", It.IsAny<CancellationToken>()))
            .Returns((string identifier, CancellationToken token) =>
            {
                WaitForCancellation(token);
                token.ThrowIfCancellationRequested();
                return new ValueTask<object>(new object());
            });
        jsRuntime.Setup(s => s.SetValueAsync("someProperty", "value", It.IsAny<CancellationToken>()))
            .Returns((string identifier, string value, CancellationToken token) =>
            {
                WaitForCancellation(token);
                token.ThrowIfCancellationRequested();
                return ValueTask.CompletedTask;
            });

        var timeout = TimeSpan.FromMilliseconds(100);
        Action invoke = operation switch
        {
            "Get" => () => jsRuntime.Object.GetValueAsync<object>("someProperty", timeout),
            "Set" => () => jsRuntime.Object.SetValueAsync("someProperty", "value", timeout),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

        var exception = Assert.Throws<JSException>(invoke);
        Assert.Equal("The JS interop call 'someProperty' timed out after 100ms.", exception.Message);
        Assert.IsType<OperationCanceledException>(exception.InnerException);

        static void WaitForCancellation(CancellationToken token)
        {
            Assert.True(token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
        }
    }

    [Fact]
    public async Task InvokeAsync_WithInfiniteTimeout()
    {
        // Arrange
        var expected = "Hello";
        var method = "someMethod";
        var args = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<string>(method, It.IsAny<CancellationToken>(), args))
            .Callback<string, CancellationToken, object[]>((method, cts, args) =>
            {
                Assert.False(cts.CanBeCanceled);
                Assert.True(cts == CancellationToken.None);
            })
            .Returns(new ValueTask<string>(expected));

        // Act
        var result = await jsRuntime.Object.InvokeAsync<string>(method, Timeout.InfiniteTimeSpan, args);

        Assert.Equal(expected, result);
        jsRuntime.Verify();
    }

    [Fact]
    public async Task InvokeVoidAsync_WithTimeout()
    {
        // Arrange
        var method = "someMethod";
        var args = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<IJSVoidResult>(method, It.IsAny<CancellationToken>(), args))
            .Callback<string, CancellationToken, object[]>((method, cts, args) =>
            {
                // There isn't a very good way to test when the cts will cancel. We'll just verify that
                // it'll get cancelled eventually.
                Assert.True(cts.CanBeCanceled);
            })
            .Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));

        // Act
        await jsRuntime.Object.InvokeVoidAsync(method, TimeSpan.FromMinutes(5), args);

        jsRuntime.Verify();
    }

    [Fact]
    public async Task InvokeVoidAsync_WithInfiniteTimeout()
    {
        // Arrange
        var method = "someMethod";
        var args = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.InvokeAsync<IJSVoidResult>(method, It.IsAny<CancellationToken>(), args))
            .Callback<string, CancellationToken, object[]>((method, cts, args) =>
            {
                Assert.False(cts.CanBeCanceled);
                Assert.True(cts == CancellationToken.None);
            })
            .Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));

        // Act
        await jsRuntime.Object.InvokeVoidAsync(method, Timeout.InfiniteTimeSpan, args);

        jsRuntime.Verify();
    }
}
