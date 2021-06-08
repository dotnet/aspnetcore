// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class RedirectActionResultTest
    {
        [Fact]
        public void RedirectResult_Constructor_WithParameterUrl_SetsResultUrlAndNotPermanentOrPreserveMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url);

            // Assert
            Assert.False(result.PreserveMethod);
            Assert.False(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void RedirectResult_Constructor_WithParameterUrlAndPermanent_SetsResultUrlAndPermanentNotPreserveMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url, permanent: true);

            // Assert
            Assert.False(result.PreserveMethod);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void RedirectResult_Constructor_WithParameterUrlPermanentAndPreservesMethod_SetsResultUrlPermanentAndPreservesMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url, permanent: true, preserveMethod: true);

            // Assert
            Assert.True(result.PreserveMethod);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Theory]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public async Task Execute_ReturnsContentPath_WhenItDoesNotStartWithTilde(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            var action
                = new Func<RedirectResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseRedirectResultTest.Execute_ReturnsContentPath_WhenItDoesNotStartWithTilde(
                appRoot,
                contentPath,
                expectedPath,
                action);
        }

        [Theory]
        [InlineData(null, "~/Home/About", "/Home/About")]
        [InlineData("/", "~/Home/About", "/Home/About")]
        [InlineData("/", "~/", "/")]
        [InlineData("", "~/Home/About", "/Home/About")]
        [InlineData("/myapproot", "~/", "/myapproot/")]
        public async Task Execute_ReturnsAppRelativePath_WhenItStartsWithTilde(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            var action =
                new Func<RedirectResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseRedirectResultTest.Execute_ReturnsAppRelativePath_WhenItStartsWithTilde(
                appRoot,
                contentPath,
                expectedPath,
                action);
        }
    }
}
