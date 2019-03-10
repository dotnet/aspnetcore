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
            Options = new HeaderPropagationOptions();
            State = new HeaderPropagationState();
            Middleware = new HeaderPropagationMiddleware(Next,
                new OptionsWrapper<HeaderPropagationOptions>(Options),
                State);
        }

        public DefaultHttpContext Context { get; set; }
        public RequestDelegate Next { get; set; }
        public HeaderPropagationOptions Options { get; set; }
        public HeaderPropagationState State { get; set; }
        public HeaderPropagationMiddleware Middleware { get; set; }

        [Fact]
        public async Task HeaderInRequest_AddCorrectValue()
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry {InputName = "in", OutputName = "out"});
            Context.Request.Headers.Add("in", "test");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("out", State.Headers.Keys);
            Assert.Equal(new[] {"test"}, State.Headers["out"]);
        }

        [Fact]
        public async Task HeaderInRequest_NoOutputName_UseInputName()
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry { InputName = "in" });
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
            Options.Headers.Add(new HeaderPropagationEntry {InputName = "in", OutputName = "out"});

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
            Options.Headers.Add(new HeaderPropagationEntry {InputName = "in", OutputName = "out"});
            Options.Headers.Add(new HeaderPropagationEntry {InputName = "another", OutputName = "more"});
            Context.Request.Headers.Add("in", "test");
            Context.Request.Headers.Add("another", "test2");

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("out", State.Headers.Keys);
            Assert.Equal(new[] {"test"}, State.Headers["out"]);
            Assert.Contains("more", State.Headers.Keys);
            Assert.Equal(new[] {"test2"}, State.Headers["more"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task HeaderEmptyInRequest_DoesNotAddIt(string headerValue)
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry {InputName = "in", OutputName = "out"});
            Context.Request.Headers.Add("in", headerValue);

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.DoesNotContain("out", State.Headers.Keys);
        }

        [Theory]
        [InlineData(new[] {"default"}, new[] {"default"})]
        [InlineData(new[] {"default", "other"}, new[] {"default", "other"})]
        public async Task NoHeaderInRequest_AddsDefaultValue(string[] defaultValues,
            string[] expectedValues)
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry
                {InputName = "in", OutputName = "out", DefaultValues = defaultValues});

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("out", State.Headers.Keys);
            Assert.Equal(expectedValues, State.Headers["out"]);
        }

        [Theory]
        [InlineData(new[] {"default"}, new[] {"default"})]
        [InlineData(new[] {"default", "other"}, new[] {"default", "other"})]
        public async Task NoHeaderInRequest_UsesDefaultValuesGenerator(string[] defaultValues,
            string[] expectedValues)
        {
            // Arrange
            HttpContext receivedContext = null;
            Options.Headers.Add(new HeaderPropagationEntry
            {
                InputName = "in",
                OutputName = "out",
                DefaultValues = "no",
                DefaultValuesGenerator = ctx =>
                {
                    receivedContext = ctx;
                    return defaultValues;
                }
            });

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.Contains("out", State.Headers.Keys);
            Assert.Equal(expectedValues, State.Headers["out"]);
            Assert.Same(Context, receivedContext);
        }

        [Fact]
        public async Task NoHeaderInRequest_EmptyDefaultValuesGenerated_DoesNotAddIt()
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry
            {
                InputName = "in",
                OutputName = "out",
                DefaultValuesGenerator = ctx => StringValues.Empty
            });

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.DoesNotContain("out", State.Headers.Keys);
        }
    }
}
