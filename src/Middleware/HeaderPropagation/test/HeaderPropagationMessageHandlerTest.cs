// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationMessageHandlerTest
    {
        public HeaderPropagationMessageHandlerTest()
        {
            Handler = new SimpleHandler();

            State = new HeaderPropagationState();
            Options = new HeaderPropagationOptions();

            var headerPropagationMessageHandler =
                new HeaderPropagationMessageHandler(new OptionsWrapper<HeaderPropagationOptions>(Options), State)
                {
                    InnerHandler = Handler
                };

            Client = new HttpClient(headerPropagationMessageHandler)
            {
                BaseAddress = new Uri("http://example.com")
            };
        }

        private SimpleHandler Handler { get; }
        private HeaderPropagationEntry Configuration { get; }
        public HeaderPropagationState State { get; set; }
        public HeaderPropagationOptions Options { get; set; }
        public HttpClient Client { get; set; }

        [Fact]
        public async Task HeaderInState_AddCorrectValue()
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry {OutputName = "out"});
            State.Headers.Add("out", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(new[] {"test"}, Handler.Headers.GetValues("out"));
        }

        [Fact]
        public async Task NoHeaderInState_DoesNotAddIt()
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry {OutputName = "out"});

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.Empty(Handler.Headers);
        }

        [Fact]
        public async Task HeaderInState_NotInOptions_DoesNotAddIt()
        {
            // Arrange
            State.Headers.Add("out", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.Empty(Handler.Headers);
        }

        [Fact]
        public async Task MultipleHeadersInState_AddsAll()
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry {OutputName = "out"});
            Options.Headers.Add(new HeaderPropagationEntry {OutputName = "another"});
            State.Headers.Add("out", "test");
            State.Headers.Add("another", "test2");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.True(Handler.Headers.Contains("another"));
            Assert.Equal(new[] {"test"}, Handler.Headers.GetValues("out"));
            Assert.Equal(new[] {"test2"}, Handler.Headers.GetValues("another"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task HeaderEmptyInState_DoNotAddIt(string headerValue)
        {
            // Arrange
            Options.Headers.Add(new HeaderPropagationEntry {OutputName = "out"});
            State.Headers.Add("out", headerValue);

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.False(Handler.Headers.Contains("out"));
        }

        [Theory]
        [InlineData(false, "", new[] {""})]
        [InlineData(false, null, new[] {""})]
        [InlineData(false, "42", new[] {"42"})]
        [InlineData(true, "42", new[] {"42", "test"})]
        [InlineData(true, "", new[] {"", "test"})]
        [InlineData(true, null, new[] {"", "test"})]
        public async Task HeaderInState_HeaderAlreadyInOutgoingRequest(bool alwaysAdd, string outgoingValue,
            string[] expectedValues)
        {
            // Arrange
            State.Headers.Add("out", "test");
            Options.Headers.Add(
                new HeaderPropagationEntry {OutputName = "out", AlwaysAdd = alwaysAdd});

            var request = new HttpRequestMessage();
            request.Headers.Add("out", outgoingValue);

            // Act
            await Client.SendAsync(request);

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(expectedValues, Handler.Headers.GetValues("out"));
        }

        private class SimpleHandler : DelegatingHandler
        {
            public HttpHeaders Headers { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Headers = request.Headers;
                return Task.FromResult(new HttpResponseMessage());
            }
        }
    }
}
