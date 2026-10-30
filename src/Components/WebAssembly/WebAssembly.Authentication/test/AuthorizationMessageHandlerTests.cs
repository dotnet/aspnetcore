// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public class AuthorizationMessageHandlerTests
{
    [Fact]
    public async Task Throws_IfTheListOfAllowedUrlsIsNotConfigured()
    {
        // Arrange
        var expectedMessage = "The 'AuthorizationMessageHandler' is not configured. " +
                "Call 'ConfigureHandler' and provide a list of endpoint urls to attach the token to.";

        var tokenProvider = new Mock<IAccessTokenProvider>();

        var handler = new AuthorizationMessageHandler(tokenProvider.Object, Mock.Of<NavigationManager>());
        // Act & Assert

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new HttpClient(handler).GetAsync("https://www.example.com"));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task DoesNotAttachTokenToRequest_IfNotPresentInListOfAllowedUrls()
    {
        // Arrange
        var tokenProvider = new Mock<IAccessTokenProvider>();

        var handler = new AuthorizationMessageHandler(tokenProvider.Object, Mock.Of<NavigationManager>());
        handler.ConfigureHandler(new[] { "https://localhost:5001" });

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        handler.InnerHandler = new TestMessageHandler(response);

        // Act
        _ = await new HttpClient(handler).GetAsync("https://www.example.com");

        // Assert
        tokenProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequestsTokenWithDefaultScopes_WhenNoTokenIsAvailable()
    {
        // Arrange
        var tokenProvider = new Mock<IAccessTokenProvider>();
        tokenProvider.Setup(tp => tp.RequestAccessToken())
                .Returns(new ValueTask<AccessTokenResult>(new AccessTokenResult(AccessTokenResultStatus.Success,
                new AccessToken
                {
                    Expires = DateTime.Now.AddHours(1),
                    GrantedScopes = new string[] { "All" },
                    Value = "asdf"
                },
                null,
                null)));

        var handler = new AuthorizationMessageHandler(tokenProvider.Object, Mock.Of<NavigationManager>());
        handler.ConfigureHandler(new[] { "https://localhost:5001" });

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        handler.InnerHandler = new TestMessageHandler(response);

        // Act
        _ = await new HttpClient(handler).GetAsync("https://localhost:5001/weather");

        // Assert
        Assert.Equal("asdf", response.RequestMessage.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task CachesExistingTokenWhenPossible()
    {
        // Arrange
        var tokenProvider = new Mock<IAccessTokenProvider>();
        tokenProvider.Setup(tp => tp.RequestAccessToken())
                .Returns(new ValueTask<AccessTokenResult>(new AccessTokenResult(AccessTokenResultStatus.Success,
                new AccessToken
                {
                    Expires = DateTime.Now.AddHours(1),
                    GrantedScopes = new string[] { "All" },
                    Value = "asdf"
                },
                null,
                null)));

        var handler = new AuthorizationMessageHandler(tokenProvider.Object, Mock.Of<NavigationManager>());
        handler.ConfigureHandler(new[] { "https://localhost:5001" });

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        handler.InnerHandler = new TestMessageHandler(response);

        // Act
        _ = await new HttpClient(handler).GetAsync("https://localhost:5001/weather");
        response.RequestMessage = null;

        _ = await new HttpClient(handler).GetAsync("https://localhost:5001/weather");

        // Assert
        Assert.Single(tokenProvider.Invocations);
        Assert.Equal("asdf", response.RequestMessage.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task RequestNewTokenWhenCurrentTokenIsAboutToExpire()
    {
        // Arrange
        var tokenProvider = new Mock<IAccessTokenProvider>();
        tokenProvider.Setup(tp => tp.RequestAccessToken())
                .Returns(new ValueTask<AccessTokenResult>(new AccessTokenResult(AccessTokenResultStatus.Success,
                new AccessToken
                {
                    Expires = DateTime.Now.AddMinutes(3),
                    GrantedScopes = new string[] { "All" },
                    Value = "asdf"
                },
                null,
                null)));

        var handler = new AuthorizationMessageHandler(tokenProvider.Object, Mock.Of<NavigationManager>());
        handler.ConfigureHandler(new[] { "https://localhost:5001" });

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        handler.InnerHandler = new TestMessageHandler(response);

        // Act
        _ = await new HttpClient(handler).GetAsync("https://localhost:5001/weather");
        response.RequestMessage = null;

        _ = await new HttpClient(handler).GetAsync("https://localhost:5001/weather");

        // Assert
        Assert.Equal(2, tokenProvider.Invocations.Count);
    }

    [Fact]
    public async Task ThrowsWhenItCanNotProvisionANewToken()
    {
        // Arrange
        var tokenProvider = new Mock<IAccessTokenProvider>();
        tokenProvider.Setup(tp => tp.RequestAccessToken())
                .Returns(new ValueTask<AccessTokenResult>(new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect,
                null,
                "authentication/login",
                new InteractiveRequestOptions { Interaction = InteractionType.GetToken, ReturnUrl = "https://www.example.com" })));

        var handler = new AuthorizationMessageHandler(tokenProvider.Object, Mock.Of<NavigationManager>());
        handler.ConfigureHandler(new[] { "https://localhost:5001" });

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        handler.InnerHandler = new TestMessageHandler(response);

        // Act & assert
        var exception = await Assert.ThrowsAsync<AccessTokenNotAvailableException>(() => new HttpClient(handler).GetAsync("https://localhost:5001/weather"));
    }

    [Fact]
    public async Task UsesCustomScopesAndReturnUrlWhenProvided()
    {
        // Arrange
        var tokenProvider = new Mock<IAccessTokenProvider>();
        tokenProvider.Setup(tp => tp.RequestAccessToken(It.IsAny<AccessTokenRequestOptions>()))
            .Returns(new ValueTask<AccessTokenResult>(new AccessTokenResult(AccessTokenResultStatus.Success,
            new AccessToken
            {
                Expires = DateTime.Now.AddMinutes(3),
                GrantedScopes = new string[] { "All" },
                Value = "asdf"
            },
            null,
            null)));

        var handler = new AuthorizationMessageHandler(tokenProvider.Object, Mock.Of<NavigationManager>());
        handler.ConfigureHandler(
            new[] { "https://localhost:5001" },
            scopes: new[] { "example.read", "example.write" },
            returnUrl: "https://www.example.com/return");

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        handler.InnerHandler = new TestMessageHandler(response);

        // Act
        _ = await new HttpClient(handler).GetAsync("https://localhost:5001/weather");

        // Assert
        Assert.Single(tokenProvider.Invocations);
    }

    [Fact]
    public async Task DoesNotCacheToken_WhenAuthenticationStateChangesWhileTokenIsBeingRequested()
    {
        // Arrange
        var staleTokenResult = new TaskCompletionSource<AccessTokenResult>();
        var tokenProvider = new TestAccessTokenProvider(
            staleTokenResult.Task,
            Task.FromResult(CreateTokenResult("fresh")));

        var handler = new AuthorizationMessageHandler(tokenProvider, Mock.Of<NavigationManager>());
        handler.ConfigureHandler(new[] { "https://localhost:5001" });

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        handler.InnerHandler = new TestMessageHandler(response);
        var client = new HttpClient(handler);

        // Act
        var firstRequest = client.GetAsync("https://localhost:5001/weather");
        tokenProvider.RaiseAuthenticationStateChanged();
        staleTokenResult.SetResult(CreateTokenResult("stale"));
        _ = await firstRequest;
        var firstRequestToken = response.RequestMessage.Headers.Authorization.Parameter;

        _ = await client.GetAsync("https://localhost:5001/weather");

        // Assert
        Assert.Equal("stale", firstRequestToken);
        Assert.Equal("fresh", response.RequestMessage.Headers.Authorization.Parameter);
    }

    private static AccessTokenResult CreateTokenResult(string value) => new(
        AccessTokenResultStatus.Success,
        new AccessToken { Expires = DateTime.Now.AddHours(1), GrantedScopes = new[] { "All" }, Value = value },
        null,
        null);

    private sealed class TestAccessTokenProvider : AuthenticationStateProvider, IAccessTokenProvider
    {
        private readonly Queue<Task<AccessTokenResult>> _results;

        public TestAccessTokenProvider(params Task<AccessTokenResult>[] results) => _results = new(results);

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

        public void RaiseAuthenticationStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

        public ValueTask<AccessTokenResult> RequestAccessToken() => new(_results.Dequeue());

        public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options) => RequestAccessToken();
    }
}

internal class TestMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public TestMessageHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _response.RequestMessage = request;
        return Task.FromResult(_response);
    }
}
