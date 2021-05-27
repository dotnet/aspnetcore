// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class RedirectResultTest
    {
        [Theory]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public async Task Execute_ReturnsContentPath_WhenItDoesNotStartWithTilde(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            var action
                = new Func<RedirectResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

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
            var action
                = new Func<RedirectResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseRedirectResultTest.Execute_ReturnsAppRelativePath_WhenItStartsWithTilde(
                appRoot,
                contentPath,
                expectedPath,
                action);
        }
    }
}
