// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests;

public class HeaderPropagationMessageHandlerTest
{
    public HeaderPropagationMessageHandlerTest()
    {
        Handler = new SimpleHandler();

        State = new HeaderPropagationValues();
        State.Headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        Configuration = new HeaderPropagationMessageHandlerOptions();

        var headerPropagationMessageHandler =
            new HeaderPropagationMessageHandler(Configuration, State)
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
    public HeaderPropagationMessageHandlerOptions Configuration { get; set; }
    public HttpClient Client { get; set; }

    [Fact]
    public async Task HeaderInState_AddCorrectValue()
    {
        // Arrange
        Configuration.Headers.Add("out");
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
        Configuration.Headers.Add("out");
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
        Configuration.Headers.Add("Content-Type");
        State.Headers.Add("Content-Type", "test");

        // Act
        await Client.SendAsync(new HttpRequestMessage() { Content = new StringContent("test") });

        // Assert
        Assert.True(Handler.Content.Headers.Contains("Content-Type"));
        Assert.Equal(new[] { "text/plain; charset=utf-8" }, Handler.Content.Headers.GetValues("Content-Type"));
    }

    [Fact]
    public async Task HeaderInState_RequestWithContent_ContentHeaderNotPresent_AddValue()
    {
        Configuration.Headers.Add("Content-Language");
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
        Configuration.Headers.Add("Content-Language");
        State.Headers.Add("Content-Language", new[] { "one", "two" });

        // Act
        await Client.SendAsync(new HttpRequestMessage() { Content = new StringContent("test") });

        // Assert
        Assert.True(Handler.Content.Headers.Contains("Content-Language"));
        Assert.Equal(new[] { "one", "two" }, Handler.Content.Headers.GetValues("Content-Language"));
    }

    [Fact]
    public async Task HeaderInState_WithOutboundName_UseOutboundName()
    {
        // Arrange
        Configuration.Headers.Add("state", "out");
        State.Headers.Add("state", "test");

        // Act
        await Client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.True(Handler.Headers.Contains("out"));
        Assert.Equal(new[] { "test" }, Handler.Headers.GetValues("out"));
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
    public async Task HeaderInState_HeaderAlreadyInOutgoingRequest_DoesNotOverrideIt(string outgoingValue,
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

    [Fact]
    public async Task HeaderInState_HeaderTwiceInOptions_DoesNotAddItTwice()
    {
        // Arrange
        State.Headers.Add("name", "value");
        Configuration.Headers.Add("name");
        Configuration.Headers.Add("name");

        // Act
        await Client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.True(Handler.Headers.Contains("name"));
        Assert.Equal(new[] { "value" }, Handler.Headers.GetValues("name"));
    }

    [Fact]
    public async Task HeaderInState_HeaderTwiceInOptionsWithDifferentNames_AddsBoth()
    {
        // Arrange
        State.Headers.Add("name", "value");
        Configuration.Headers.Add("name");
        Configuration.Headers.Add("name", "other");

        // Act
        await Client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.True(Handler.Headers.Contains("name"));
        Assert.Equal(new[] { "value" }, Handler.Headers.GetValues("name"));
        Assert.True(Handler.Headers.Contains("other"));
        Assert.Equal(new[] { "value" }, Handler.Headers.GetValues("name"));
    }

    [Fact]
    public async Task TwoHeadersInState_BothHeadersInOptionsWithSameName_AddsFirst()
    {
        // Arrange
        State.Headers.Add("name", "value");
        State.Headers.Add("other", "override");
        Configuration.Headers.Add("name");
        Configuration.Headers.Add("other", "name");

        // Act
        await Client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.True(Handler.Headers.Contains("name"));
        Assert.Equal(new[] { "value" }, Handler.Headers.GetValues("name"));
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
