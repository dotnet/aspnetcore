// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            Next = ctx => Task.CompletedTask;
            Configuration = new HeaderPropagationOptions();
            State = new HeaderPropagationValues();
            Middleware = new HeaderPropagationMiddleware(Next,
                new OptionsWrapper<HeaderPropagationOptions>(Configuration),
                State);
        }

        public DefaultHttpContext Context { get; set; }
        public RequestDelegate Next { get; set; }
        public HeaderPropagationOptions Configuration { get; set; }
        public HeaderPropagationValues State { get; set; }
        public HeaderPropagationMiddleware Middleware { get; set; }

        [Fact]
        public async Task HeaderInRequest_AddCorrectValue()
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry());
            Context.Request.Headers.Add("in", "test");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(new[] { "test" }, State.Headers["in"]);
        }

        [Fact]
        public async Task NoHeaderInRequest_DoesNotAddIt()
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry());

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Empty(State.Headers);
        }

        [Fact]
        public async Task HeaderInRequest_NotInOptions_DoesNotAddIt()
        {
            // Arrange
            Context.Request.Headers.Add("in", "test");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Empty(State.Headers);
        }

        [Fact]
        public async Task MultipleHeadersInRequest_AddAllHeaders()
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry());
            Configuration.Headers.Add("another", new HeaderPropagationEntry());
            Context.Request.Headers.Add("in", "test");
            Context.Request.Headers.Add("another", "test2");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(new[] { "test" }, State.Headers["in"]);
            Assert.Contains("another", State.Headers.Keys);
            Assert.Equal(new[] { "test2" }, State.Headers["another"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task HeaderEmptyInRequest_DoesNotAddIt(string headerValue)
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry());
            Context.Request.Headers.Add("in", headerValue);

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.DoesNotContain("in", State.Headers.Keys);
        }

        [Theory]
        [InlineData(new[] { "default" }, new[] { "default" })]
        [InlineData(new[] { "default", "other" }, new[] { "default", "other" })]
        public async Task NoHeaderInRequest_AddsDefaultValue(string[] defaultValues,
            string[] expectedValues)
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry { DefaultValue = defaultValues });

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(expectedValues, State.Headers["in"]);
        }

        [Theory]
        [InlineData(new[] { "default" }, new[] { "default" })]
        [InlineData(new[] { "default", "other" }, new[] { "default", "other" })]
        public async Task UsesValueFactory(string[] factoryValues,
            string[] expectedValues)
        {
            // Arrange
            string receivedName = null;
            HttpContext receivedContext = null;
            Configuration.Headers.Add("in", new HeaderPropagationEntry
            {
                DefaultValue = "no",
                ValueFactory = (name, ctx) =>
                {
                    receivedName = name;
                    receivedContext = ctx;
                    return factoryValues;
                }
            });

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(expectedValues, State.Headers["in"]);
            Assert.Equal("in", receivedName);
            Assert.Same(Context, receivedContext);
        }

        [Fact]
        public async Task PreferValueFactory_OverDefaultValuesAndRequestHeader()
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry
            {
                DefaultValue = "no",
                ValueFactory = (name, ctx) => "test"
            });
            Context.Request.Headers.Add("in", "no");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal("test", State.Headers["in"]);
        }

        [Fact]
        public async Task EmptyValuesFromValueFactory_DoesNotAddIt()
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry
            {
                ValueFactory = (name, ctx) => StringValues.Empty
            });

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.DoesNotContain("in", State.Headers.Keys);
        }

        [Fact]
        public async Task NullEntryInConfiguration_HeaderInRequest_AddsCorrectValue()
        {
            // Arrange
            Configuration.Headers.Add("in", null);
            Context.Request.Headers.Add("in", "test");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(new[] { "test" }, State.Headers["in"]);
        }

        [Fact]
        public async Task NullEntryInConfiguration_NoHeaderInRequest_DoesNotAddHeader()
        {
            // Arrange
            Configuration.Headers.Add("in", null);

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.DoesNotContain("in", State.Headers.Keys);
        }
    }
}
