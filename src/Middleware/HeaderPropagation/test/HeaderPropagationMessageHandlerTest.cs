// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationMessageHandlerTest
    {
        public HeaderPropagationMessageHandlerTest()
        {
            Handler = new SimpleHandler();

            State = new HeaderPropagationValues();
            State.Headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            Configuration = new HeaderPropagationOptions();

            var headerPropagationMessageHandler =
                new HeaderPropagationMessageHandler(Options.Create(Configuration), State)
                {
                    InnerHandler = Handler
                };

            Client = new HttpClient(headerPropagationMessageHandler)
            {
                BaseAddress = new Uri("http://example.com")
            };
        }

        private SimpleHandler Handler { get; }
        public HeaderPropagationValues State { get; set; }
        public HeaderPropagationOptions Configuration { get; set; }
        public HttpClient Client { get; set; }

        [Fact]
        public async Task HeaderInState_AddCorrectValue()
        {
            // Arrange
            Configuration.Headers.Add("in", "out");
            State.Headers.Add("out", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(new[] { "test" }, Handler.Headers.GetValues("out"));
        }

        [Fact]
        public async Task HeaderInState_WithMultipleValues_AddAllValues()
        {
            // Arrange
            Configuration.Headers.Add("in", "out");
            State.Headers.Add("out", new[] { "one", "two" });

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(new[] { "one", "two" }, Handler.Headers.GetValues("out"));
        }

        [Fact]
        public async Task HeaderInState_RequestWithContent_ContentHeaderPresent_DoesNotAddIt()
        {
            Configuration.Headers.Add("in", "Content-Type");
            State.Headers.Add("in", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage() { Content = new StringContent("test") });

            // Assert
            Assert.True(Handler.Content.Headers.Contains("Content-Type"));
            Assert.Equal(new[] { "text/plain; charset=utf-8" }, Handler.Content.Headers.GetValues("Content-Type"));
        }

        [Fact]
        public async Task HeaderInState_RequestWithContent_ContentHeaderNotPresent_AddValue()
        {
            Configuration.Headers.Add("in", "Content-Language");
            State.Headers.Add("Content-Language", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage() { Content = new StringContent("test") });

            // Assert
            Assert.True(Handler.Content.Headers.Contains("Content-Language"));
            Assert.Equal(new[] { "test" }, Handler.Content.Headers.GetValues("Content-Language"));
        }

        [Fact]
        public async Task HeaderInState_WithMultipleValues_RequestWithContent_ContentHeaderNotPresent_AddAllValues()
        {
            Configuration.Headers.Add("in", "Content-Language");
            State.Headers.Add("Content-Language", new[] { "one", "two" });

            // Act
            await Client.SendAsync(new HttpRequestMessage() { Content = new StringContent("test") });

            // Assert
            Assert.True(Handler.Content.Headers.Contains("Content-Language"));
            Assert.Equal(new[] { "one", "two" }, Handler.Content.Headers.GetValues("Content-Language"));
        }

        [Fact]
        public async Task HeaderInState_NoOutputName_UseInputName()
        {
            // Arrange
            Configuration.Headers.Add("in");
            State.Headers.Add("in", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("in"));
            Assert.Equal(new[] { "test" }, Handler.Headers.GetValues("in"));
        }

        [Fact]
        public async Task NoHeaderInState_DoesNotAddIt()
        {
            // Arrange
            Configuration.Headers.Add("inout");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.Empty(Handler.Headers);
        }

        [Fact]
        public async Task HeaderInState_NotInOptions_DoesNotAddIt()
        {
            // Arrange
            State.Headers.Add("inout", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.Empty(Handler.Headers);
        }

        [Fact]
        public async Task MultipleHeadersInState_AddsAll()
        {
            // Arrange
            Configuration.Headers.Add("inout");
            Configuration.Headers.Add("another");
            State.Headers.Add("inout", "test");
            State.Headers.Add("another", "test2");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("inout"));
            Assert.True(Handler.Headers.Contains("another"));
            Assert.Equal(new[] { "test" }, Handler.Headers.GetValues("inout"));
            Assert.Equal(new[] { "test2" }, Handler.Headers.GetValues("another"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task HeaderEmptyInState_DoNotAddIt(string headerValue)
        {
            // Arrange
            Configuration.Headers.Add("inout");
            State.Headers.Add("inout", headerValue);

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.False(Handler.Headers.Contains("inout"));
        }

        [Theory]
        [InlineData("", new[] { "" })]
        [InlineData(null, new[] { "" })]
        [InlineData("42", new[] { "42" })]
        public async Task HeaderInState_HeaderAlreadyInOutgoingRequest(string outgoingValue,
            string[] expectedValues)
        {
            // Arrange
            State.Headers.Add("inout", "test");
            Configuration.Headers.Add("inout");

            var request = new HttpRequestMessage();
            request.Headers.Add("inout", outgoingValue);

            // Act
            await Client.SendAsync(request);

            // Assert
            Assert.True(Handler.Headers.Contains("inout"));
            Assert.Equal(expectedValues, Handler.Headers.GetValues("inout"));
        }

        private class SimpleHandler : DelegatingHandler
        {
            public HttpHeaders Headers { get; private set; }
            public HttpContent Content { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Headers = request.Headers;
                Content = request.Content;
                return Task.FromResult(new HttpResponseMessage());
            }
        }
    }
}
