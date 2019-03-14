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

            State = new HeaderPropagationValues();
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
            Configuration.Headers.Add("in", new HeaderPropagationEntry { OutboundHeaderName = "out" });
            State.Headers.Add("in", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(new[] { "test" }, Handler.Headers.GetValues("out"));
        }

        [Fact]
        public async Task HeaderInState_NoOutputName_UseInputName()
        {
            // Arrange
            Configuration.Headers.Add("in", new HeaderPropagationEntry());
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
            Configuration.Headers.Add("inout", new HeaderPropagationEntry());

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
            Configuration.Headers.Add("inout", new HeaderPropagationEntry());
            Configuration.Headers.Add("another", new HeaderPropagationEntry());
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
            Configuration.Headers.Add("inout", new HeaderPropagationEntry());
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
            Configuration.Headers.Add("inout", new HeaderPropagationEntry());

            var request = new HttpRequestMessage();
            request.Headers.Add("inout", outgoingValue);

            // Act
            await Client.SendAsync(request);

            // Assert
            Assert.True(Handler.Headers.Contains("inout"));
            Assert.Equal(expectedValues, Handler.Headers.GetValues("inout"));
        }

        [Fact]
        public async Task NullEntryInConfiguration_AddCorrectValue()
        {
            // Arrange
            Configuration.Headers.Add("in", null);
            State.Headers.Add("in", "test");

            // Act
            await Client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("in"));
            Assert.Equal(new[] { "test" }, Handler.Headers.GetValues("in"));
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
