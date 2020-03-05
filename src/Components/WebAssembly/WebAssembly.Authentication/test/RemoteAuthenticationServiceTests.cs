// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class RemoteAuthenticationServiceTests
    {
        [Fact]
        public async Task RemoteAuthenticationService_SignIn_UpdatesUserOnSuccess()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.SignInResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                State = state,
                Status = RemoteAuthenticationStatus.Success.ToString()
            };

            // Act
            await runtime.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { State = state });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.signIn", "AuthenticationService.getUser" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Theory]
        [InlineData(RemoteAuthenticationStatus.Redirect)]
        [InlineData(RemoteAuthenticationStatus.Failure)]
        [InlineData(RemoteAuthenticationStatus.OperationCompleted)]
        public async Task RemoteAuthenticationService_SignIn_DoesNotUpdateUserOnOtherResult(RemoteAuthenticationStatus value)
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.SignInResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = value.ToString()
            };

            // Act
            await runtime.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { State = state });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.signIn" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Fact]
        public async Task RemoteAuthenticationService_CompleteSignInAsync_UpdatesUserOnSuccess()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.CompleteSignInResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                State = state,
                Status = RemoteAuthenticationStatus.Success.ToString()
            };

            // Act
            await runtime.CompleteSignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { Url = "https://www.example.com/base/login-callback" });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.completeSignIn", "AuthenticationService.getUser" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Theory]
        [InlineData(RemoteAuthenticationStatus.Redirect)]
        [InlineData(RemoteAuthenticationStatus.Failure)]
        [InlineData(RemoteAuthenticationStatus.OperationCompleted)]
        public async Task RemoteAuthenticationService_CompleteSignInAsync_DoesNotUpdateUserOnOtherResult(RemoteAuthenticationStatus value)
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.CompleteSignInResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = value.ToString().ToString()
            };

            // Act
            await runtime.CompleteSignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { Url = "https://www.example.com/base/login-callback" });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.completeSignIn" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Fact]
        public async Task RemoteAuthenticationService_SignOut_UpdatesUserOnSuccess()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.SignOutResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                State = state,
                Status = RemoteAuthenticationStatus.Success.ToString()
            };

            // Act
            await runtime.SignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { State = state });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.signOut", "AuthenticationService.getUser" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Theory]
        [InlineData(RemoteAuthenticationStatus.Redirect)]
        [InlineData(RemoteAuthenticationStatus.Failure)]
        [InlineData(RemoteAuthenticationStatus.OperationCompleted)]
        public async Task RemoteAuthenticationService_SignOut_DoesNotUpdateUserOnOtherResult(RemoteAuthenticationStatus value)
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.SignOutResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = value.ToString()
            };

            // Act
            await runtime.SignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { State = state });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.signOut" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Fact]
        public async Task RemoteAuthenticationService_CompleteSignOutAsync_UpdatesUserOnSuccess()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.CompleteSignOutResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                State = state,
                Status = RemoteAuthenticationStatus.Success.ToString()
            };

            // Act
            await runtime.CompleteSignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { Url = "https://www.example.com/base/login-callback" });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.completeSignOut", "AuthenticationService.getUser" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Theory]
        [InlineData(RemoteAuthenticationStatus.Redirect)]
        [InlineData(RemoteAuthenticationStatus.Failure)]
        [InlineData(RemoteAuthenticationStatus.OperationCompleted)]
        public async Task RemoteAuthenticationService_CompleteSignOutAsync_DoesNotUpdateUserOnOtherResult(RemoteAuthenticationStatus value)
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.CompleteSignOutResult = new InternalRemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = value.ToString()
            };

            // Act
            await runtime.CompleteSignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { Url = "https://www.example.com/base/login-callback" });

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.completeSignOut" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Fact]
        public async Task RemoteAuthenticationService_GetAccessToken_ReturnsAccessTokenResult()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult
            {
                Status = "success",
                Token = new AccessToken
                {
                    Value = "1234",
                    GrantedScopes = new[] { "All" },
                    Expires = new DateTimeOffset(2050, 5, 13, 0, 0, 0, TimeSpan.Zero)
                }
            };

            // Act
            var result = await runtime.RequestAccessToken();

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.getAccessToken" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());

            Assert.True(result.TryGetToken(out var token));
            Assert.Equal(result.Status, Enum.Parse<AccessTokenResultStatus>(testJsRuntime.GetAccessTokenResult.Status, ignoreCase: true));
            Assert.Equal(result.RedirectUrl, testJsRuntime.GetAccessTokenResult.RedirectUrl);
            Assert.Equal(token, testJsRuntime.GetAccessTokenResult.Token);
        }

        [Fact]
        public async Task RemoteAuthenticationService_GetAccessToken_PassesDownOptions()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult
            {
                Status = "requiresRedirect",
            };

            var tokenOptions = new AccessTokenRequestOptions
            {
                Scopes = new[] { "something" }
            };

            var expectedRedirectUrl = "https://www.example.com/base/login?returnUrl=https%3A%2F%2Fwww.example.com%2Fbase%2Fadd-product";

            // Act
            var result = await runtime.RequestAccessToken(tokenOptions);

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.getAccessToken" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());

            Assert.False(result.TryGetToken(out var token));
            Assert.Null(token);
            Assert.Equal(result.Status, Enum.Parse<AccessTokenResultStatus>(testJsRuntime.GetAccessTokenResult.Status, ignoreCase: true));
            Assert.Equal(expectedRedirectUrl, result.RedirectUrl);
            Assert.Equal(tokenOptions, (AccessTokenRequestOptions)testJsRuntime.PastInvocations[^1].args[0]);
        }

        [Fact]
        public async Task RemoteAuthenticationService_GetAccessToken_ComputesDefaultReturnUrlOnRequiresRedirect()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var state = new RemoteAuthenticationState();
            testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult
            {
                Status = "requiresRedirect",
            };

            var tokenOptions = new AccessTokenRequestOptions
            {
                Scopes = new[] { "something" },
                ReturnUrl = "https://www.example.com/base/add-saved-product/123413241234"
            };

            var expectedRedirectUrl = "https://www.example.com/base/login?returnUrl=https%3A%2F%2Fwww.example.com%2Fbase%2Fadd-saved-product%2F123413241234";

            // Act
            var result = await runtime.RequestAccessToken(tokenOptions);

            // Assert
            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.getAccessToken" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());

            Assert.False(result.TryGetToken(out var token));
            Assert.Null(token);
            Assert.Equal(result.Status, Enum.Parse<AccessTokenResultStatus>(testJsRuntime.GetAccessTokenResult.Status, ignoreCase: true));
            Assert.Equal(expectedRedirectUrl, result.RedirectUrl);
            Assert.Equal(tokenOptions, (AccessTokenRequestOptions)testJsRuntime.PastInvocations[^1].args[0]);
        }

        [Fact]
        public async Task RemoteAuthenticationService_GetUser_ReturnsAnonymousClaimsPrincipal_ForUnauthenticatedUsers()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            testJsRuntime.GetUserResult = null;

            // Act
            var result = await runtime.GetAuthenticatedUser();

            // Assert
            Assert.Empty(result.Claims);
            Assert.Single(result.Identities);
            Assert.False(result.Identity.IsAuthenticated);

            Assert.Equal(
                new[] { "AuthenticationService.init", "AuthenticationService.getUser" },
                testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());
        }

        [Fact]
        public async Task RemoteAuthenticationService_GetUser_ReturnsUser_ForAuthenticatedUsers()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions();
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var serializationOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
            var serializedUser = JsonSerializer.Serialize(new
            {
                CoolName = "Alfred",
                CoolRole = new[] { "admin", "cool", "fantastic" }
            }, serializationOptions);

            testJsRuntime.GetUserResult = JsonSerializer.Deserialize<IDictionary<string, object>>(serializedUser);

            // Act
            var result = await runtime.GetAuthenticatedUser();

            // Assert
            Assert.Single(result.Identities);
            Assert.True(result.Identity.IsAuthenticated);
            Assert.Equal("Alfred", result.Identity.Name);
            Assert.Equal("a", result.Identity.AuthenticationType);
            Assert.True(result.IsInRole("admin"));
            Assert.True(result.IsInRole("cool"));
            Assert.True(result.IsInRole("fantastic"));
        }

        [Fact]
        public async Task RemoteAuthenticationService_GetUser_DoesNotMapScopesToRoles()
        {
            // Arrange
            var testJsRuntime = new TestJsRuntime();
            var options = CreateOptions("scope");
            var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, OidcProviderOptions>(
                testJsRuntime,
                options,
                new TestNavigationManager());

            var serializationOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
            var serializedUser = JsonSerializer.Serialize(new
            {
                CoolName = "Alfred",
                CoolRole = new[] { "admin", "cool", "fantastic" }
            }, serializationOptions);

            testJsRuntime.GetUserResult = JsonSerializer.Deserialize<IDictionary<string, object>>(serializedUser);
            testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult
            {
                Status = "success",
                Token = new AccessToken
                {
                    Value = "1234",
                    GrantedScopes = new[] { "All" },
                    Expires = new DateTimeOffset(2050, 5, 13, 0, 0, 0, TimeSpan.Zero)
                }
            };

            // Act
            var result = await runtime.GetAuthenticatedUser();

            // Assert
            Assert.Single(result.Identities);
            Assert.True(result.Identity.IsAuthenticated);
            Assert.Equal("Alfred", result.Identity.Name);
            Assert.Equal("a", result.Identity.AuthenticationType);
            Assert.True(result.IsInRole("admin"));
            Assert.True(result.IsInRole("cool"));
            Assert.True(result.IsInRole("fantastic"));
            Assert.Empty(result.FindAll("scope"));
        }

        private static IOptions<RemoteAuthenticationOptions<OidcProviderOptions>> CreateOptions(string scopeClaim = null)
        {
            return Options.Create(
                new RemoteAuthenticationOptions<OidcProviderOptions>()
                {
                    AuthenticationPaths = new RemoteAuthenticationApplicationPathsOptions
                    {
                        LogInPath = "login",
                        LogInCallbackPath = "a",
                        LogInFailedPath = "a",
                        RegisterPath = "a",
                        ProfilePath = "a",
                        RemoteRegisterPath = "a",
                        RemoteProfilePath = "a",
                        LogOutPath = "a",
                        LogOutCallbackPath = "a",
                        LogOutFailedPath = "a",
                        LogOutSucceededPath = "a",
                    },
                    UserOptions = new RemoteAuthenticationUserOptions
                    {
                        AuthenticationType = "a",
                        ScopeClaim = scopeClaim,
                        RoleClaim = "coolRole",
                        NameClaim = "coolName",
                    },
                    ProviderOptions = new OidcProviderOptions
                    {
                        Authority = "a",
                        ClientId = "a",
                        DefaultScopes = new[] { "openid" },
                        RedirectUri = "https://www.example.com/base/custom-login",
                        PostLogoutRedirectUri = "https://www.example.com/base/custom-logout",
                    }
                });
        }

        private class TestJsRuntime : IJSRuntime
        {
            public IList<(string identifier, object[] args)> PastInvocations { get; set; } = new List<(string, object[])>();

            public InternalRemoteAuthenticationResult<RemoteAuthenticationState> SignInResult { get; set; }

            public InternalRemoteAuthenticationResult<RemoteAuthenticationState> CompleteSignInResult { get; set; }

            public InternalRemoteAuthenticationResult<RemoteAuthenticationState> SignOutResult { get; set; }

            public InternalRemoteAuthenticationResult<RemoteAuthenticationState> CompleteSignOutResult { get; set; }

            public InternalAccessTokenResult GetAccessTokenResult { get; set; }

            public IDictionary<string, object> GetUserResult { get; set; }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
            {
                PastInvocations.Add((identifier, args));
                return new ValueTask<TValue>((TValue)GetInvocationResult<TValue>(identifier));
            }


            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
            {
                PastInvocations.Add((identifier, args));
                return new ValueTask<TValue>((TValue)GetInvocationResult<TValue>(identifier));
            }

            private object GetInvocationResult<TValue>(string identifier)
            {
                switch (identifier)
                {
                    case "AuthenticationService.init":
                        return default;
                    case "AuthenticationService.signIn":
                        return SignInResult;
                    case "AuthenticationService.completeSignIn":
                        return CompleteSignInResult;
                    case "AuthenticationService.signOut":
                        return SignOutResult;
                    case "AuthenticationService.completeSignOut":
                        return CompleteSignOutResult;
                    case "AuthenticationService.getAccessToken":
                        return GetAccessTokenResult;
                    case "AuthenticationService.getUser":
                        return GetUserResult;
                    default:
                        break;
                }

                return default;
            }
        }
    }

    internal class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() =>
            Initialize("https://www.example.com/base/", "https://www.example.com/base/add-product");

        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotImplementedException();
    }
}
