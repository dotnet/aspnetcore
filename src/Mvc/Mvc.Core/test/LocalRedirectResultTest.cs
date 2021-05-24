// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class LocalRedirectResultTest
    {
        [Fact]
        public async Task Execute_ReturnsExpectedValues()
        {
            var action = new Func<LocalRedirectResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

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
            var action = new Func<LocalRedirectResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

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
            var action = new Func<LocalRedirectResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseLocalRedirectResultTest.Execute_Throws_ForNonLocalUrlTilde(appRoot, contentPath, action);
        }
    }
}
