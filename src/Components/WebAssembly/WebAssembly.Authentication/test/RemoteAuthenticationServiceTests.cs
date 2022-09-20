// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationServiceTests
{
    [Fact]
    public async Task RemoteAuthenticationService_SignIn_UpdatesUserOnSuccess()
    {
        // Arrange
        var testJsRuntime = new TestJsRuntime();
        var options = CreateOptions();
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.SignInResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            State = state,
            Status = RemoteAuthenticationStatus.Success
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.SignInResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            Status = value
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.CompleteSignInResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            State = state,
            Status = RemoteAuthenticationStatus.Success
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.CompleteSignInResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            Status = value
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.SignOutResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            State = state,
            Status = RemoteAuthenticationStatus.Success
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.SignOutResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            Status = value
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.CompleteSignOutResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            State = state,
            Status = RemoteAuthenticationStatus.Success
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.CompleteSignOutResult = new RemoteAuthenticationResult<RemoteAuthenticationState>
        {
            Status = value
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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult
        {
            Status = AccessTokenResultStatus.Success,
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
        Assert.Equal(result.Status, testJsRuntime.GetAccessTokenResult.Status);
        Assert.Equal(token, testJsRuntime.GetAccessTokenResult.Token);
    }

    [Fact]
    public async Task RemoteAuthenticationService_GetAccessToken_PassesDownOptions()
    {
        // Arrange
        var testJsRuntime = new TestJsRuntime();
        var options = CreateOptions();
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult(AccessTokenResultStatus.RequiresRedirect, null);

        var tokenOptions = new AccessTokenRequestOptions
        {
            Scopes = new[] { "something" }
        };

        // Act
        var result = await runtime.RequestAccessToken(tokenOptions);

        // Assert
        Assert.Equal(
            new[] { "AuthenticationService.init", "AuthenticationService.getAccessToken" },
            testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());

        Assert.False(result.TryGetToken(out var token));
        Assert.Null(token);
        Assert.Equal(result.Status, testJsRuntime.GetAccessTokenResult.Status);
        Assert.Equal("login", result.InteractiveRequestUrl);
        Assert.Equal("https://www.example.com/base/add-product", result.InteractionOptions.ReturnUrl);
        Assert.Equal(new[] { "something" }, result.InteractionOptions.Scopes);
        Assert.Equal(tokenOptions, (AccessTokenRequestOptions)testJsRuntime.PastInvocations[^1].args[0]);
    }

    [Fact]
    public async Task RemoteAuthenticationService_GetAccessToken_ComputesDefaultReturnUrlOnRequiresRedirect()
    {
        // Arrange
        var testJsRuntime = new TestJsRuntime();
        var options = CreateOptions();
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var state = new RemoteAuthenticationState();
        testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult
        {
            Status = AccessTokenResultStatus.RequiresRedirect,
        };

        var tokenOptions = new AccessTokenRequestOptions
        {
            Scopes = new[] { "something" },
            ReturnUrl = "https://www.example.com/base/add-saved-product/123413241234"
        };

        // Act
        var result = await runtime.RequestAccessToken(tokenOptions);

        // Assert
        Assert.Equal(
            new[] { "AuthenticationService.init", "AuthenticationService.getAccessToken" },
            testJsRuntime.PastInvocations.Select(i => i.identifier).ToArray());

        Assert.False(result.TryGetToken(out var token));
        Assert.Null(token);
        Assert.Equal(result.Status, testJsRuntime.GetAccessTokenResult.Status);
        Assert.Equal("login", result.InteractiveRequestUrl);
        Assert.Equal("https://www.example.com/base/add-saved-product/123413241234", result.InteractionOptions.ReturnUrl);
        Assert.Equal(new[] { "something" }, result.InteractionOptions.Scopes);
        Assert.Equal(tokenOptions, (AccessTokenRequestOptions)testJsRuntime.PastInvocations[^1].args[0]);
    }

    [Fact]
    public async Task RemoteAuthenticationService_GetUser_ReturnsAnonymousClaimsPrincipal_ForUnauthenticatedUsers()
    {
        // Arrange
        var testJsRuntime = new TestJsRuntime();
        var options = CreateOptions();
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        testJsRuntime.GetUserResult = default;

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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, CoolRoleAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new TestAccountClaimsPrincipalFactory(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var account = new CoolRoleAccount
        {
            CoolRole = new[] { "admin", "cool", "fantastic" },
            AdditionalProperties = new Dictionary<string, object>
            {
                ["CoolName"] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize("Alfred"))
            }
        };

        testJsRuntime.GetUserResult = account;

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
        var runtime = new RemoteAuthenticationService<RemoteAuthenticationState, CoolRoleAccount, OidcProviderOptions>(
            testJsRuntime,
            options,
            new TestNavigationManager(),
            new TestAccountClaimsPrincipalFactory(Mock.Of<IAccessTokenProviderAccessor>()),
            null);

        var account = new CoolRoleAccount
        {
            CoolRole = new[] { "admin", "cool", "fantastic" },
            AdditionalProperties = new Dictionary<string, object>
            {
                ["CoolName"] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize("Alfred")),
            }
        };

        testJsRuntime.GetUserResult = account;
        testJsRuntime.GetAccessTokenResult = new InternalAccessTokenResult
        {
            Status = AccessTokenResultStatus.Success,
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

    private static IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> CreateOptions(string scopeClaim = null)
    {
        var options = new RemoteAuthenticationOptions<OidcProviderOptions>();

        options.AuthenticationPaths.LogInPath = "login";
        options.AuthenticationPaths.LogInCallbackPath = "a";
        options.AuthenticationPaths.LogInFailedPath = "a";
        options.AuthenticationPaths.RegisterPath = "a";
        options.AuthenticationPaths.ProfilePath = "a";
        options.AuthenticationPaths.RemoteRegisterPath = "a";
        options.AuthenticationPaths.RemoteProfilePath = "a";
        options.AuthenticationPaths.LogOutPath = "a";
        options.AuthenticationPaths.LogOutCallbackPath = "a";
        options.AuthenticationPaths.LogOutFailedPath = "a";
        options.AuthenticationPaths.LogOutSucceededPath = "a";
        options.UserOptions.AuthenticationType = "a";
        options.UserOptions.ScopeClaim = scopeClaim;
        options.UserOptions.RoleClaim = "coolRole";
        options.UserOptions.NameClaim = "coolName";
        options.ProviderOptions.Authority = "a";
        options.ProviderOptions.ClientId = "a";
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.RedirectUri = "https://www.example.com/base/custom-login";
        options.ProviderOptions.PostLogoutRedirectUri = "https://www.example.com/base/custom-logout";

        var iOptions = Options.Create(options);

        var mock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        mock.Setup(m => m.Value).Returns(options);
        return mock.Object;
    }

    private class TestJsRuntime : IJSRuntime
    {
        public IList<(string identifier, object[] args)> PastInvocations { get; set; } = new List<(string, object[])>();

        public RemoteAuthenticationResult<RemoteAuthenticationState> SignInResult { get; set; }

        public RemoteAuthenticationResult<RemoteAuthenticationState> CompleteSignInResult { get; set; }

        public RemoteAuthenticationResult<RemoteAuthenticationState> SignOutResult { get; set; }

        public RemoteAuthenticationResult<RemoteAuthenticationState> CompleteSignOutResult { get; set; }

        public InternalAccessTokenResult GetAccessTokenResult { get; set; }

        public RemoteUserAccount GetUserResult { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            PastInvocations.Add((identifier, args));
            return new ValueTask<TValue>((TValue)GetInvocationResult(identifier));
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            PastInvocations.Add((identifier, args));
            return new ValueTask<TValue>((TValue)GetInvocationResult(identifier));
        }

        private object GetInvocationResult(string identifier)
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

internal class TestAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<CoolRoleAccount>
{
    public TestAccountClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) : base(accessor)
    {
    }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        CoolRoleAccount account,
        RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        if (account.CoolRole != null)
        {
            foreach (var role in account.CoolRole)
            {
                ((ClaimsIdentity)user.Identity).AddClaim(new Claim("CoolRole", role));
            }
        }

        return user;
    }
}

internal class CoolRoleAccount : RemoteUserAccount
{
    public string[] CoolRole { get; set; }
}

internal class TestNavigationManager : NavigationManager
{
    public TestNavigationManager() =>
        Initialize("https://www.example.com/base/", "https://www.example.com/base/add-product");

    protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotImplementedException();
}
