// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class LocalRedirectActionResultTest
    {
        [Fact]
        public void Constructor_WithParameterUrl_SetsResultUrlAndNotPermanentOrPreserveMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new LocalRedirectResult(url);

            // Assert
            Assert.False(result.PreserveMethod);
            Assert.False(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void Constructor_WithParameterUrlAndPermanent_SetsResultUrlAndPermanentNotPreserveMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new LocalRedirectResult(url, permanent: true);

            // Assert
            Assert.False(result.PreserveMethod);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void Constructor_WithParameterUrlAndPermanent_SetsResultUrlPermanentAndPreserveMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new LocalRedirectResult(url, permanent: true, preserveMethod: true);

            // Assert
            Assert.True(result.PreserveMethod);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public async Task Execute_ReturnsExpectedValues()
        {
            var action = new Func<LocalRedirectResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseLocalRedirectResultTest.Execute_ReturnsExpectedValues(action);
        }

        [Theory]
        [InlineData("", "//")]
        [InlineData("", "/\\")]
        [InlineData("", "//foo")]
        [InlineData("", "/\\foo")]
        [InlineData("", "Home/About")]
        [InlineData("/myapproot", "http://www.example.com")]
        public async Task Execute_Throws_ForNonLocalUrl(
            string appRoot,
            string contentPath)
        {
            var action = new Func<LocalRedirectResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseLocalRedirectResultTest.Execute_Throws_ForNonLocalUrl(appRoot, contentPath, action);
        }

        [Theory]
        [InlineData("", "~//")]
        [InlineData("", "~/\\")]
        [InlineData("", "~//foo")]
        [InlineData("", "~/\\foo")]
        public async Task Execute_Throws_ForNonLocalUrlTilde(
            string appRoot,
            string contentPath)
        {
            var action = new Func<LocalRedirectResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseLocalRedirectResultTest.Execute_Throws_ForNonLocalUrlTilde(appRoot, contentPath, action);
        }
    }
}
