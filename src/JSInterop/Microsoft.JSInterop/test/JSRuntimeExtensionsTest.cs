// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.JSInterop
{
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
            jsRuntime.Setup(s => s.InvokeAsync<object>(method, args)).Returns(new ValueTask<object>(new object()));

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
            jsRuntime.Setup(s => s.InvokeAsync<object>(method, It.IsAny<CancellationToken>(), args)).Returns(new ValueTask<object>(new object()));

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
            jsRuntime.Setup(s => s.InvokeAsync<object>(method, It.IsAny<CancellationToken>(), args))
                .Callback<string, CancellationToken, object[]>((method, cts, args) =>
                {
                    // There isn't a very good way to test when the cts will cancel. We'll just verify that
                    // it'll get cancelled eventually.
                    Assert.True(cts.CanBeCanceled);
                })
                .Returns(new ValueTask<object>(new object()));

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
            jsRuntime.Setup(s => s.InvokeAsync<object>(method, It.IsAny<CancellationToken>(), args))
                .Callback<string, CancellationToken, object[]>((method, cts, args) =>
                {
                    Assert.False(cts.CanBeCanceled);
                    Assert.True(cts == CancellationToken.None);
                })
                .Returns(new ValueTask<object>(new object()));

            // Act
            await jsRuntime.Object.InvokeVoidAsync(method, Timeout.InfiniteTimeSpan, args);

            jsRuntime.Verify();
        }
    }
}
