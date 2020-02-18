// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationMiddlewareTest
    {
        public HeaderPropagationMiddlewareTest()
        {
            Context = new DefaultHttpContext();
            Next = ctx =>
            {
                CapturedHeaders = State.Headers;
                return Task.CompletedTask;
            };
            Configuration = new HeaderPropagationOptions();
            State = new HeaderPropagationValues();
            Middleware = new HeaderPropagationMiddleware(Next,
                new OptionsWrapper<HeaderPropagationOptions>(Configuration),
                State);
        }

        public DefaultHttpContext Context { get; set; }
        public RequestDelegate Next { get; set; }
        public Action Assertion { get; set; }
        public HeaderPropagationOptions Configuration { get; set; }
        public HeaderPropagationValues State { get; set; }
        public IDictionary<string, StringValues> CapturedHeaders { get; set; }
        public HeaderPropagationMiddleware Middleware { get; set; }

        [Fact]
        public async Task HeaderInRequest_AddCorrectValue()
        {
            // Arrange
            Configuration.Headers.Add("in");
            Context.Request.Headers.Add("in", "test");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", CapturedHeaders.Keys);
            Assert.Equal(new[] { "test" }, CapturedHeaders["in"]);
        }

        [Fact]
        public async Task NoHeaderInRequest_DoesNotAddIt()
        {
            // Arrange
            Configuration.Headers.Add("in");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Empty(CapturedHeaders);
        }

        [Fact]
        public async Task HeaderInRequest_NotInOptions_DoesNotAddIt()
        {
            // Arrange
            Context.Request.Headers.Add("in", "test");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Empty(CapturedHeaders);
        }

        [Fact]
        public async Task MultipleHeadersInRequest_AddAllHeaders()
        {
            // Arrange
            Configuration.Headers.Add("in");
            Configuration.Headers.Add("another");
            Context.Request.Headers.Add("in", "test");
            Context.Request.Headers.Add("another", "test2");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", CapturedHeaders.Keys);
            Assert.Equal(new[] { "test" }, CapturedHeaders["in"]);
            Assert.Contains("another", CapturedHeaders.Keys);
            Assert.Equal(new[] { "test2" }, CapturedHeaders["another"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task HeaderEmptyInRequest_DoesNotAddIt(string headerValue)
        {
            // Arrange
            Configuration.Headers.Add("in");
            Context.Request.Headers.Add("in", headerValue);

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.DoesNotContain("in", CapturedHeaders.Keys);
        }

        [Theory]
        [InlineData(new[] { "default" }, new[] { "default" })]
        [InlineData(new[] { "default", "other" }, new[] { "default", "other" })]
        public async Task UsesValueFilter(string[] filterValues, string[] expectedValues)
        {
            // Arrange
            string receivedName = null;
            StringValues receivedValue = default;
            HttpContext receivedContext = null;
            Configuration.Headers.Add("in", context =>
            {
                receivedValue = context.HeaderValue;
                receivedName = context.HeaderName;
                receivedContext = context.HttpContext;
                return filterValues;
            });

            Context.Request.Headers.Add("in", "value");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", CapturedHeaders.Keys);
            Assert.Equal(expectedValues, CapturedHeaders["in"]);
            Assert.Equal("in", receivedName);
            Assert.Equal(new StringValues("value"), receivedValue);
            Assert.Same(Context, receivedContext);
        }

        [Fact]
        public async Task PreferValueFilter_OverRequestHeader()
        {
            // Arrange
            Configuration.Headers.Add("in", context => "test");
            Context.Request.Headers.Add("in", "no");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", CapturedHeaders.Keys);
            Assert.Equal("test", CapturedHeaders["in"]);
        }

        [Fact]
        public async Task EmptyValuesFromValueFilter_DoesNotAddIt()
        {
            // Arrange
            Configuration.Headers.Add("in", (context) => StringValues.Empty);

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.DoesNotContain("in", CapturedHeaders.Keys);
        }

        [Fact]
        public async Task MultipleEntries_AddsFirstToProduceValue()
        {
            // Arrange
            Configuration.Headers.Add("in");
            Configuration.Headers.Add("in", (context) => StringValues.Empty);
            Configuration.Headers.Add("in", (context) => "Test");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", CapturedHeaders.Keys);
            Assert.Equal("Test", CapturedHeaders["in"]);
        }

        [Fact]
        public async Task HeaderInRequest_WithBleedAsyncLocal_HasCorrectValue()
        {
            // Arrange
            Configuration.Headers.Add("in");

            // Process first request
            Context.Request.Headers.Add("in", "dirty");
            await Middleware.Invoke(Context);

            // Process second request
            Context = new DefaultHttpContext();
            Context.Request.Headers.Add("in", "test");
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", CapturedHeaders.Keys);
            Assert.Equal(new[] { "test" }, CapturedHeaders["in"]);
        }

        [Fact]
        public async Task NoHeaderInRequest_WithBleedAsyncLocal_DoesNotHaveIt()
        {
            // Arrange
            Configuration.Headers.Add("in");

            // Process first request
            Context.Request.Headers.Add("in", "dirty");
            await Middleware.Invoke(Context);

            // Process second request
            Context = new DefaultHttpContext();
            await Middleware.Invoke(Context);

            // Assert
            Assert.Empty(CapturedHeaders);
        }
    }
}
