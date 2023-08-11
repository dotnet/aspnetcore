// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class SignInManagerTest
{
    [Fact]
    public void ConstructorNullChecks()
    {
        Assert.Throws<ArgumentNullException>("userManager", () => new SignInManager<PocoUser>(null, null, null, null, null, null, null));
        var userManager = MockHelpers.MockUserManager<PocoUser>().Object;
        Assert.Throws<ArgumentNullException>("contextAccessor", () => new SignInManager<PocoUser>(userManager, null, null, null, null, null, null));
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
        Assert.Throws<ArgumentNullException>("claimsFactory", () => new SignInManager<PocoUser>(userManager, contextAccessor.Object, null, null, null, null, null));
    }

    [Fact]
    public async Task PasswordSignInReturnsLockedOutWhenLockedOut()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true).Verifiable();

        var context = new Mock<HttpContext>();
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
        var roleManager = MockHelpers.MockRoleManager<PocoRole>();
        var identityOptions = new IdentityOptions();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(a => a.Value).Returns(identityOptions);
        var claimsFactory = new UserClaimsPrincipalFactory<PocoUser, PocoRole>(manager.Object, roleManager.Object, options.Object);
        var logger = new TestLogger<SignInManager<PocoUser>>();
        var helper = new SignInManager<PocoUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object, logger, new Mock<IAuthenticationSchemeProvider>().Object, new DefaultUserConfirmation<PocoUser>());

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-bogus1", false, false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsLockedOut);
        Assert.Contains($"User is currently locked out.", logger.LogMessages);
        manager.Verify();
    }

    [Fact]
    public async Task CheckPasswordSignInReturnsLockedOutWhenLockedOut()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true).Verifiable();

        var context = new Mock<HttpContext>();
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
        var roleManager = MockHelpers.MockRoleManager<PocoRole>();
        var identityOptions = new IdentityOptions();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(a => a.Value).Returns(identityOptions);
        var claimsFactory = new UserClaimsPrincipalFactory<PocoUser, PocoRole>(manager.Object, roleManager.Object, options.Object);
        var logger = new TestLogger<SignInManager<PocoUser>>();
        var helper = new SignInManager<PocoUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object, logger, new Mock<IAuthenticationSchemeProvider>().Object, new DefaultUserConfirmation<PocoUser>());

        // Act
        var result = await helper.CheckPasswordSignInAsync(user, "[PLACEHOLDER]-bogus1", false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsLockedOut);
        Assert.Contains($"User is currently locked out.", logger.LogMessages);
        manager.Verify();
    }

    private static Mock<UserManager<PocoUser>> SetupUserManager(PocoUser user)
    {
        var manager = MockHelpers.MockUserManager<PocoUser>();
        manager.Setup(m => m.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        manager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        manager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id.ToString());
        manager.Setup(m => m.GetUserNameAsync(user)).ReturnsAsync(user.UserName);
        return manager;
    }

    private static SignInManager<PocoUser> SetupSignInManager(UserManager<PocoUser> manager, HttpContext context, ILogger logger = null, IdentityOptions identityOptions = null, IAuthenticationSchemeProvider schemeProvider = null)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(context);
        var roleManager = MockHelpers.MockRoleManager<PocoRole>();
        identityOptions = identityOptions ?? new IdentityOptions();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(a => a.Value).Returns(identityOptions);
        var claimsFactory = new UserClaimsPrincipalFactory<PocoUser, PocoRole>(manager, roleManager.Object, options.Object);
        schemeProvider = schemeProvider ?? new MockSchemeProvider();
        var sm = new SignInManager<PocoUser>(manager, contextAccessor.Object, claimsFactory, options.Object, null, schemeProvider, new DefaultUserConfirmation<PocoUser>());
        sm.Logger = logger ?? NullLogger<SignInManager<PocoUser>>.Instance;
        return sm;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanPasswordSignIn(bool isPersistent)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        SetupSignIn(context, auth, user.Id, isPersistent, loginProvider: null, amr: "pwd");
        var helper = SetupSignInManager(manager.Object, context);

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-1a", isPersistent, false);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task CanPasswordSignInWithNoLogger()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        SetupSignIn(context, auth, user.Id, false, loginProvider: null, amr: "pwd");
        var helper = SetupSignInManager(manager.Object, context);

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-1a", false, false);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task PasswordSignInWorksWithNonTwoFactorStore()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        SetupSignIn(context, auth);
        var helper = SetupSignInManager(manager.Object, context);

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-1a", false, false);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task CheckPasswordOnlyResetLockoutWhenTfaNotEnabledOrRemembered(bool tfaEnabled, bool tfaRemembered)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.SupportsUserTwoFactor).Returns(tfaEnabled).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);

        if (tfaEnabled)
        {
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user)).ReturnsAsync(new string[1] { "Fake" }).Verifiable();
        }

        if (tfaRemembered)
        {
            var id = new ClaimsIdentity(IdentityConstants.TwoFactorRememberMeScheme);
            id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
            auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorRememberMeScheme))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), null, IdentityConstants.TwoFactorRememberMeScheme))).Verifiable();
        }

        if (!tfaEnabled || tfaRemembered)
        {
            manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();
        }

        // Act
        var helper = SetupSignInManager(manager.Object, context);
        var result = await helper.CheckPasswordSignInAsync(user, "[PLACEHOLDER]-1a", false);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
    }

    [Fact]
    public async Task CheckPasswordAlwaysResetLockoutWhenQuirked()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Identity.CheckPasswordSignInAlwaysResetLockoutOnSuccess", true);

        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();

        var context = new DefaultHttpContext();
        var helper = SetupSignInManager(manager.Object, context);

        // Act
        var result = await helper.CheckPasswordSignInAsync(user, "[PLACEHOLDER]-1a", false);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();

        AppContext.SetSwitch("Microsoft.AspNetCore.Identity.CheckPasswordSignInAlwaysResetLockoutOnSuccess", false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PasswordSignInRequiresVerification(bool supportsLockout)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
        if (supportsLockout)
        {
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        }
        IList<string> providers = new List<string>();
        providers.Add("PhoneNumber");
        manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user)).Returns(Task.FromResult(providers)).Verifiable();
        manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
        manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user)).ReturnsAsync(new string[1] { "Fake" }).Verifiable();
        var context = new DefaultHttpContext();
        var helper = SetupSignInManager(manager.Object, context);
        var auth = MockAuth(context);
        auth.Setup(a => a.SignInAsync(context, IdentityConstants.TwoFactorUserIdScheme,
            It.Is<ClaimsPrincipal>(id => id.FindFirstValue(ClaimTypes.Name) == user.Id),
            It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-1a", false, false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.RequiresTwoFactor);
        manager.Verify();
        auth.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExternalSignInRequiresVerificationIfNotBypassed(bool bypass)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        const string loginProvider = "login";
        const string providerKey = "fookey";
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(false).Verifiable();
        manager.Setup(m => m.FindByLoginAsync(loginProvider, providerKey)).ReturnsAsync(user).Verifiable();
        if (!bypass)
        {
            IList<string> providers = new List<string>();
            providers.Add("PhoneNumber");
            manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user)).Returns(Task.FromResult(providers)).Verifiable();
            manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true).Verifiable();
        }
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);

        if (bypass)
        {
            SetupSignIn(context, auth, user.Id, false, loginProvider);
        }
        else
        {
            auth.Setup(a => a.SignInAsync(context, IdentityConstants.TwoFactorUserIdScheme,
                It.Is<ClaimsPrincipal>(id => id.FindFirstValue(ClaimTypes.Name) == user.Id),
                It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        }

        // Act
        var result = await helper.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent: false, bypassTwoFactor: bypass);

        // Assert
        Assert.Equal(bypass, result.Succeeded);
        Assert.Equal(!bypass, result.RequiresTwoFactor);
        manager.Verify();
        auth.Verify();
    }

    private class GoodTokenProvider : AuthenticatorTokenProvider<PocoUser>
    {
        public override Task<bool> ValidateAsync(string purpose, string token, UserManager<PocoUser> manager, PocoUser user)
        {
            return Task.FromResult(true);
        }
    }

    [Theory]
    [InlineData(null, true, true)]
    [InlineData("Authenticator", false, true)]
    [InlineData("Gooblygook", true, false)]
    [InlineData("--", false, false)]
    public async Task CanTwoFactorAuthenticatorSignIn(string providerName, bool isPersistent, bool rememberClient)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        const string code = "3123";
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, providerName ?? TokenOptions.DefaultAuthenticatorProvider, code)).ReturnsAsync(true).Verifiable();
        manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var twoFactorInfo = new SignInManager<PocoUser>.TwoFactorAuthenticationInfo { User = user };
        if (providerName != null)
        {
            helper.Options.Tokens.AuthenticatorTokenProvider = providerName;
        }
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, null);
        SetupSignIn(context, auth, user.Id, isPersistent);
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();
        if (rememberClient)
        {
            auth.Setup(a => a.SignInAsync(context,
                IdentityConstants.TwoFactorRememberMeScheme,
                It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.Name) == user.Id
                    && i.Identities.First().AuthenticationType == IdentityConstants.TwoFactorRememberMeScheme),
                It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        }

        // Act
        var result = await helper.TwoFactorAuthenticatorSignInAsync(code, isPersistent, rememberClient);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task TwoFactorAuthenticatorSignInFailWithoutLockout()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        string providerName = "Authenticator";
        const string code = "3123";
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(false).Verifiable();
        manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, providerName ?? TokenOptions.DefaultAuthenticatorProvider, code)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.AccessFailedAsync(user)).Throws(new Exception("Should not get called"));

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var twoFactorInfo = new SignInManager<PocoUser>.TwoFactorAuthenticationInfo { User = user };
        if (providerName != null)
        {
            helper.Options.Tokens.AuthenticatorTokenProvider = providerName;
        }
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, null);
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();

        // Act
        var result = await helper.TwoFactorAuthenticatorSignInAsync(code, isPersistent: false, rememberClient: false);

        // Assert
        Assert.False(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task TwoFactorAuthenticatorSignInAsyncReturnsLockedOut()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        string providerName = "Authenticator";
        const string code = "3123";
        var manager = SetupUserManager(user);
        var lockedout = false;
        manager.Setup(m => m.AccessFailedAsync(user)).Returns(() =>
        {
            lockedout = true;
            return Task.FromResult(IdentityResult.Success);
        }).Verifiable();
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, providerName ?? TokenOptions.DefaultAuthenticatorProvider, code)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).Returns(() => Task.FromResult(lockedout));

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var twoFactorInfo = new SignInManager<PocoUser>.TwoFactorAuthenticationInfo { User = user };
        if (providerName != null)
        {
            helper.Options.Tokens.AuthenticatorTokenProvider = providerName;
        }
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, null);
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();

        // Act
        var result = await helper.TwoFactorAuthenticatorSignInAsync(code, isPersistent: false, rememberClient: false);

        // Assert
        Assert.True(result.IsLockedOut);
        manager.Verify();
        auth.Verify();
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    public async Task IsTwoFactorEnabled(bool userManagerSupportsTwoFactor, bool userTwoFactorEnabled, bool hasValidProviders)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserTwoFactor).Returns(userManagerSupportsTwoFactor).Verifiable();
        if (userManagerSupportsTwoFactor)
        {
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(userTwoFactorEnabled).Verifiable();
            if (userTwoFactorEnabled)
            {
                manager
                    .Setup(m => m.GetValidTwoFactorProvidersAsync(user))
                    .ReturnsAsync(hasValidProviders ? new string[1] { "Fake" } : Array.Empty<string>())
                    .Verifiable();
            }
        }

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);

        // Act
        var result = await helper.IsTwoFactorEnabledAsync(user);

        // Assert
        var expected = userManagerSupportsTwoFactor && userTwoFactorEnabled && hasValidProviders;
        Assert.Equal(expected, result);
        manager.Verify();
        auth.Verify();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CanTwoFactorRecoveryCodeSignIn(bool supportsLockout, bool externalLogin)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        const string bypassCode = "someCode";
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
        manager.Setup(m => m.RedeemTwoFactorRecoveryCodeAsync(user, bypassCode)).ReturnsAsync(IdentityResult.Success).Verifiable();
        if (supportsLockout)
        {
            manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();
        }
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var twoFactorInfo = new SignInManager<PocoUser>.TwoFactorAuthenticationInfo { User = user };
        var loginProvider = "loginprovider";
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, externalLogin ? loginProvider : null);
        if (externalLogin)
        {
            auth.Setup(a => a.SignInAsync(context,
                IdentityConstants.ApplicationScheme,
                It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.AuthenticationMethod) == loginProvider
                    && i.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id),
                It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
            auth.Setup(a => a.SignOutAsync(context, IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
            auth.Setup(a => a.SignOutAsync(context, IdentityConstants.TwoFactorUserIdScheme, It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        }
        else
        {
            SetupSignIn(context, auth, user.Id);
        }
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();

        // Act
        var result = await helper.TwoFactorRecoveryCodeSignInAsync(bypassCode);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CanExternalSignIn(bool isPersistent, bool supportsLockout)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        const string loginProvider = "login";
        const string providerKey = "fookey";
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
        if (supportsLockout)
        {
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        }
        manager.Setup(m => m.FindByLoginAsync(loginProvider, providerKey)).ReturnsAsync(user).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        SetupSignIn(context, auth, user.Id, isPersistent, loginProvider);

        // Act
        var result = await helper.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CanResignIn(
        // Suppress warning that says theory methods should use all of their parameters.
        // See comments below about why this isn't used.
#pragma warning disable xUnit1026
        bool isPersistent,
#pragma warning restore xUnit1026
        bool externalLogin)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var loginProvider = "loginprovider";
        var id = new ClaimsIdentity();
        if (externalLogin)
        {
            id.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
        }
        // REVIEW: auth changes we lost the ability to mock is persistent
        //var properties = new AuthenticationProperties { IsPersistent = isPersistent };
        var authResult = AuthenticateResult.NoResult();
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.ApplicationScheme))
            .Returns(Task.FromResult(authResult)).Verifiable();
        var manager = SetupUserManager(user);
        var signInManager = new Mock<SignInManager<PocoUser>>(manager.Object,
            new HttpContextAccessor { HttpContext = context },
            new Mock<IUserClaimsPrincipalFactory<PocoUser>>().Object,
            null, null, new Mock<IAuthenticationSchemeProvider>().Object, null)
        { CallBase = true };
        //signInManager.Setup(s => s.SignInAsync(user, It.Is<AuthenticationProperties>(p => p.IsPersistent == isPersistent),
        //externalLogin? loginProvider : null)).Returns(Task.FromResult(0)).Verifiable();
        signInManager.Setup(s => s.SignInWithClaimsAsync(user, It.IsAny<AuthenticationProperties>(), It.IsAny<IEnumerable<Claim>>())).Returns(Task.FromResult(0)).Verifiable();
        signInManager.Object.Context = context;

        // Act
        await signInManager.Object.RefreshSignInAsync(user);

        // Assert
        auth.Verify();
        signInManager.Verify();
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, true, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, false, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, false)]
    public async Task CanTwoFactorSignIn(bool isPersistent, bool supportsLockout, bool externalLogin, bool rememberClient)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        var provider = "twofactorprovider";
        var code = "123456";
        manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
        if (supportsLockout)
        {
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();
        }
        manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, code)).ReturnsAsync(true).Verifiable();
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var twoFactorInfo = new SignInManager<PocoUser>.TwoFactorAuthenticationInfo { User = user };
        var loginProvider = "loginprovider";
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, externalLogin ? loginProvider : null);
        if (externalLogin)
        {
            auth.Setup(a => a.SignInAsync(context,
                IdentityConstants.ApplicationScheme,
                It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.AuthenticationMethod) == loginProvider
                    && i.FindFirstValue("amr") == "mfa"
                    && i.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id),
                It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
            // REVIEW: restore ability to test is persistent
            //It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Verifiable();
            auth.Setup(a => a.SignOutAsync(context, IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
            auth.Setup(a => a.SignOutAsync(context, IdentityConstants.TwoFactorUserIdScheme, It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        }
        else
        {
            SetupSignIn(context, auth, user.Id, isPersistent, null, "mfa");
        }
        if (rememberClient)
        {
            auth.Setup(a => a.SignInAsync(context,
                IdentityConstants.TwoFactorRememberMeScheme,
                It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.Name) == user.Id
                    && i.Identities.First().AuthenticationType == IdentityConstants.TwoFactorRememberMeScheme),
                It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
            //It.Is<AuthenticationProperties>(v => v.IsPersistent == true))).Returns(Task.FromResult(0)).Verifiable();
        }
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();

        // Act
        var result = await helper.TwoFactorSignInAsync(provider, code, isPersistent, rememberClient);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task TwoFactorSignInAsyncReturnsLockedOut()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        var provider = "twofactorprovider";
        var code = "123456";
        var lockedout = false;
        manager.Setup(m => m.AccessFailedAsync(user)).Returns(() =>
        {
            lockedout = true;
            return Task.FromResult(IdentityResult.Success);
        }).Verifiable();
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).Returns(() => Task.FromResult(lockedout));
        manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, code)).ReturnsAsync(false).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, loginProvider: null);

        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();

        // Act
        var result = await helper.TwoFactorSignInAsync(provider, code, isPersistent: false, rememberClient: false);

        // Assert
        Assert.True(result.IsLockedOut);
        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task RememberClientStoresUserId()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        auth.Setup(a => a.SignInAsync(
            context,
            IdentityConstants.TwoFactorRememberMeScheme,
            It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.Name) == user.Id
                && i.Identities.First().AuthenticationType == IdentityConstants.TwoFactorRememberMeScheme),
            It.Is<AuthenticationProperties>(v => v.IsPersistent == true))).Returns(Task.FromResult(0)).Verifiable();

        // Act
        await helper.RememberTwoFactorClientAsync(user);

        // Assert
        manager.Verify();
        auth.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RememberBrowserSkipsTwoFactorVerificationSignIn(bool isPersistent)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true).Verifiable();
        IList<string> providers = new List<string>();
        providers.Add("PhoneNumber");
        manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user)).Returns(Task.FromResult(providers)).Verifiable();
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        SetupSignIn(context, auth);
        var id = new ClaimsIdentity(IdentityConstants.TwoFactorRememberMeScheme);
        id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorRememberMeScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), null, IdentityConstants.TwoFactorRememberMeScheme))).Verifiable();
        var helper = SetupSignInManager(manager.Object, context);

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-1a", isPersistent, false);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    private Mock<IAuthenticationService> MockAuth(HttpContext context)
    {
        var auth = new Mock<IAuthenticationService>();
        context.RequestServices = new ServiceCollection().AddSingleton(auth.Object).BuildServiceProvider();
        return auth;
    }

    [Fact]
    public async Task SignOutCallsContextResponseSignOut()
    {
        // Setup
        var manager = MockHelpers.TestUserManager<PocoUser>();
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        auth.Setup(a => a.SignOutAsync(context, IdentityConstants.ApplicationScheme, It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        auth.Setup(a => a.SignOutAsync(context, IdentityConstants.TwoFactorUserIdScheme, It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        auth.Setup(a => a.SignOutAsync(context, IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        var helper = SetupSignInManager(manager, context, null, manager.Options);

        // Act
        await helper.SignOutAsync();

        // Assert
        auth.Verify();
    }

    [Fact]
    public async Task PasswordSignInFailsWithWrongPassword()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-bogus1")).ReturnsAsync(false).Verifiable();
        var context = new Mock<HttpContext>();
        var logger = new TestLogger<SignInManager<PocoUser>>();
        var helper = SetupSignInManager(manager.Object, context.Object, logger);

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-bogus1", false, false);
        var checkResult = await helper.CheckPasswordSignInAsync(user, "[PLACEHOLDER]-bogus1", false);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(checkResult.Succeeded);
        Assert.Contains($"User failed to provide the correct password.", logger.LogMessages);
        manager.Verify();
        context.Verify();
    }

    [Fact]
    public async Task PasswordSignInFailsWithUnknownUser()
    {
        // Setup
        var manager = MockHelpers.MockUserManager<PocoUser>();
        manager.Setup(m => m.FindByNameAsync("unknown-username")).ReturnsAsync(default(PocoUser)).Verifiable();
        var context = new Mock<HttpContext>();
        var helper = SetupSignInManager(manager.Object, context.Object);

        // Act
        var result = await helper.PasswordSignInAsync("unknown-username", "[PLACEHOLDER]-bogus1", false, false);

        // Assert
        Assert.False(result.Succeeded);
        manager.Verify();
        context.Verify();
    }

    [Fact]
    public async Task PasswordSignInFailsWithWrongPasswordCanAccessFailedAndLockout()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        var lockedout = false;
        manager.Setup(m => m.AccessFailedAsync(user)).Returns(() =>
        {
            lockedout = true;
            return Task.FromResult(IdentityResult.Success);
        }).Verifiable();
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).Returns(() => Task.FromResult(lockedout));
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-bogus1")).ReturnsAsync(false).Verifiable();
        var context = new Mock<HttpContext>();
        var helper = SetupSignInManager(manager.Object, context.Object);

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-bogus1", false, true);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsLockedOut);
        manager.Verify();
    }

    [Fact]
    public async Task CheckPasswordSignInFailsWithWrongPasswordCanAccessFailedAndLockout()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        var lockedout = false;
        manager.Setup(m => m.AccessFailedAsync(user)).Returns(() =>
        {
            lockedout = true;
            return Task.FromResult(IdentityResult.Success);
        }).Verifiable();
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).Returns(() => Task.FromResult(lockedout));
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-bogus1")).ReturnsAsync(false).Verifiable();
        var context = new Mock<HttpContext>();
        var helper = SetupSignInManager(manager.Object, context.Object);

        // Act
        var result = await helper.CheckPasswordSignInAsync(user, "[PLACEHOLDER]-bogus1", true);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsLockedOut);
        manager.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanRequireConfirmedEmailForPasswordSignIn(bool confirmed)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(confirmed).Verifiable();
        if (confirmed)
        {
            manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        }
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        if (confirmed)
        {
            manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
            SetupSignIn(context, auth, user.Id, isPersistent: null, loginProvider: null, amr: "pwd");
        }
        var identityOptions = new IdentityOptions();
        identityOptions.SignIn.RequireConfirmedEmail = true;
        var logger = new TestLogger<SignInManager<PocoUser>>();
        var helper = SetupSignInManager(manager.Object, context, logger, identityOptions);

        // Act
        var result = await helper.PasswordSignInAsync(user, "[PLACEHOLDER]-1a", false, false);

        // Assert

        Assert.Equal(confirmed, result.Succeeded);
        Assert.NotEqual(confirmed, result.IsNotAllowed);

        var message = $"User cannot sign in without a confirmed email.";
        if (!confirmed)
        {
            Assert.Contains(message, logger.LogMessages);
        }
        else
        {
            Assert.DoesNotContain(message, logger.LogMessages);
        }

        manager.Verify();
        auth.Verify();
    }

    private static void SetupSignIn(HttpContext context, Mock<IAuthenticationService> auth, string userId = null, bool? isPersistent = null, string loginProvider = null, string amr = null)
    {
        auth.Setup(a => a.SignInAsync(context,
            IdentityConstants.ApplicationScheme,
            It.Is<ClaimsPrincipal>(id =>
                (userId == null || id.FindFirstValue(ClaimTypes.NameIdentifier) == userId) &&
                (loginProvider == null || id.FindFirstValue(ClaimTypes.AuthenticationMethod) == loginProvider) &&
                (amr == null || id.FindFirstValue("amr") == amr)),
            It.Is<AuthenticationProperties>(v => isPersistent == null || v.IsPersistent == isPersistent))).Returns(Task.FromResult(0)).Verifiable();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanRequireConfirmedPhoneNumberForPasswordSignIn(bool confirmed)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.IsPhoneNumberConfirmedAsync(user)).ReturnsAsync(confirmed).Verifiable();
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        if (confirmed)
        {
            manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
            SetupSignIn(context, auth, user.Id, isPersistent: null, loginProvider: null, amr: "pwd");
        }

        var identityOptions = new IdentityOptions();
        identityOptions.SignIn.RequireConfirmedPhoneNumber = true;
        var logger = new TestLogger<SignInManager<PocoUser>>();
        var helper = SetupSignInManager(manager.Object, context, logger, identityOptions);

        // Act
        var result = await helper.PasswordSignInAsync(user, "[PLACEHOLDER]-1a", false, false);

        // Assert
        Assert.Equal(confirmed, result.Succeeded);
        Assert.NotEqual(confirmed, result.IsNotAllowed);

        var message = $"User cannot sign in without a confirmed phone number.";
        if (!confirmed)
        {
            Assert.Contains(message, logger.LogMessages);
        }
        else
        {
            Assert.DoesNotContain(message, logger.LogMessages);
        }

        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task GetExternalLoginInfoAsyncReturnsCorrectProviderDisplayName()
    {
        // Arrange
        var user = new PocoUser { Id = "foo", UserName = "Foo" };
        var userManager = SetupUserManager(user);
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "bar"));
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        properties.Items["LoginProvider"] = "blah";
        var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, properties, "blah"));
        var auth = MockAuth(context);
        auth.Setup(s => s.AuthenticateAsync(context, IdentityConstants.ExternalScheme)).ReturnsAsync(authResult);
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        var handler = new Mock<IAuthenticationHandler>();
        schemeProvider.Setup(s => s.GetAllSchemesAsync())
            .ReturnsAsync(new[]
            {
                new AuthenticationScheme("blah", "Blah blah", handler.Object.GetType())
            });
        var signInManager = SetupSignInManager(userManager.Object, context, schemeProvider: schemeProvider.Object);

        // Act
        var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();

        // Assert
        Assert.Equal("Blah blah", externalLoginInfo.ProviderDisplayName);
    }

    [Fact]
    public async Task GetExternalLoginInfoAsyncWithOidcSubClaim()
    {
        // Arrange
        var user = new PocoUser { Id = "foo", UserName = "Foo" };
        var userManager = SetupUserManager(user);
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("sub", "bar"));
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        properties.Items["LoginProvider"] = "blah";
        var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, properties, "blah"));
        var auth = MockAuth(context);
        auth.Setup(s => s.AuthenticateAsync(context, IdentityConstants.ExternalScheme)).ReturnsAsync(authResult);
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        var handler = new Mock<IAuthenticationHandler>();
        schemeProvider.Setup(s => s.GetAllSchemesAsync())
            .ReturnsAsync(new[]
            {
                new AuthenticationScheme("blah", "Blah blah", handler.Object.GetType())
            });
        var signInManager = SetupSignInManager(userManager.Object, context, schemeProvider: schemeProvider.Object);

        // Act
        var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();

        // Assert
        Assert.Equal("bar", externalLoginInfo.ProviderKey);
    }

    [Fact]
    public async Task ExternalLoginInfoAsyncReturnsAuthenticationPropertiesWithCustomValue()
    {
        // Arrange
        var user = new PocoUser { Id = "foo", UserName = "Foo" };
        var userManager = SetupUserManager(user);
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "bar"));
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        properties.Items["LoginProvider"] = "blah";
        properties.Items["CustomValue"] = "fizzbuzz";
        var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, properties, "blah"));
        var auth = MockAuth(context);
        auth.Setup(s => s.AuthenticateAsync(context, IdentityConstants.ExternalScheme)).ReturnsAsync(authResult);
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        var handler = new Mock<IAuthenticationHandler>();
        schemeProvider.Setup(s => s.GetAllSchemesAsync())
            .ReturnsAsync(new[]
                {
                    new AuthenticationScheme("blah", "Blah blah", handler.Object.GetType())
                });
        var signInManager = SetupSignInManager(userManager.Object, context, schemeProvider: schemeProvider.Object);
        var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();

        // Act
        var externalProperties = externalLoginInfo.AuthenticationProperties;
        var customValue = externalProperties?.Items["CustomValue"];

        // Assert
        Assert.NotNull(externalProperties);
        Assert.Equal("fizzbuzz", customValue);
    }

    public static object[][] SignInManagerTypeNames => new object[][]
    {
        new[] { nameof(SignInManager<PocoUser>) },
        new[] { nameof(NoOverridesSignInManager<PocoUser>) },
        new[] { nameof(OverrideAndAwaitBaseResetSignInManager<PocoUser>) },
        new[] { nameof(OverrideAndPassThroughUserManagerResetSignInManager<PocoUser>) },
    };

    [Theory]
    [MemberData(nameof(SignInManagerTypeNames))]
    public async Task CheckPasswordSignInFailsWhenResetLockoutFails(string signInManagerTypeName)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Failed()).Verifiable();

        var context = new DefaultHttpContext();
        var helper = SetupSignInManagerType(manager.Object, context, signInManagerTypeName);

        // Act
        var result = await helper.CheckPasswordSignInAsync(user, "[PLACEHOLDER]-1a", false);

        // Assert
        Assert.Same(SignInResult.Failed, result);
        manager.Verify();
    }

    [Theory]
    [MemberData(nameof(SignInManagerTypeNames))]
    public async Task PasswordSignInWorksWhenResetLockoutReturnsNullIdentityResult(string signInManagerTypeName)
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-1a")).ReturnsAsync(true).Verifiable();
        manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync((IdentityResult)null).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        SetupSignIn(context, auth);
        var helper = SetupSignInManagerType(manager.Object, context, signInManagerTypeName);

        // Act
        var result = await helper.PasswordSignInAsync(user.UserName, "[PLACEHOLDER]-1a", false, false);

        // Assert
        Assert.True(result.Succeeded);
        manager.Verify();
        auth.Verify();
    }

    [Fact]
    public async Task TwoFactorSignFailsWhenResetLockoutFails()
    {
        // Setup
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        var provider = "twofactorprovider";
        var code = "123456";
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, code)).ReturnsAsync(true).Verifiable();

        manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Failed()).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, null);
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();

        // Act
        var result = await helper.TwoFactorSignInAsync(provider, code, false, false);

        // Assert
        Assert.Same(SignInResult.Failed, result);
        manager.Verify();
        auth.Verify();
    }

    public static object[][] ExpectedLockedOutSignInResultsGivenAccessFailedResults => new object[][]
    {
        new object[] { IdentityResult.Success, SignInResult.LockedOut },
        new object[] { null, SignInResult.LockedOut },
        new object[] { IdentityResult.Failed(), SignInResult.Failed },
    };

    [Theory]
    [MemberData(nameof(ExpectedLockedOutSignInResultsGivenAccessFailedResults))]
    public async Task CheckPasswordSignInLockedOutResultIsDependentOnTheAccessFailedAsyncResult(IdentityResult accessFailedResult, SignInResult expectedSignInResult)
    {
        // Setup
        var isLockedOutCallCount = 0;
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        // Return false initially to allow the password to be checked Only return true the second time after the bogus password is checked.
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(() => isLockedOutCallCount++ > 0).Verifiable();
        manager.Setup(m => m.CheckPasswordAsync(user, "[PLACEHOLDER]-bogus1")).ReturnsAsync(false).Verifiable();
        manager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(accessFailedResult).Verifiable();

        var context = new DefaultHttpContext();
        // Since the PasswordSignInAsync calls the UserManager directly rather than a virtual SignInManager method like ResetLockout, we don't need to test derived SignInManagers.
        var helper = SetupSignInManager(manager.Object, context);

        // Act
        var result = await helper.CheckPasswordSignInAsync(user, "[PLACEHOLDER]-bogus1", lockoutOnFailure: true);

        // Assert
        Assert.Same(expectedSignInResult, result);
        manager.Verify();
    }

    [Theory]
    [MemberData(nameof(ExpectedLockedOutSignInResultsGivenAccessFailedResults))]
    public async Task TwoFactorSignInLockedOutResultIsDependentOnTheAccessFailedAsyncResult(IdentityResult accessFailedResult, SignInResult expectedSignInResult)
    {
        // Setup
        var isLockedOutCallCount = 0;
        var user = new PocoUser { UserName = "Foo" };
        var manager = SetupUserManager(user);
        var provider = "twofactorprovider";
        var code = "123456";
        manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
        // Return false initially to allow the 2fa code to be checked. Only return true if ever in the future it is called again after failure.
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(() => isLockedOutCallCount++ > 0).Verifiable();
        manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, code)).ReturnsAsync(false).Verifiable();

        manager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(accessFailedResult).Verifiable();

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        var helper = SetupSignInManager(manager.Object, context);
        var id = SignInManager<PocoUser>.StoreTwoFactorInfo(user.Id, null);
        auth.Setup(a => a.AuthenticateAsync(context, IdentityConstants.TwoFactorUserIdScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(id, null, IdentityConstants.TwoFactorUserIdScheme))).Verifiable();

        // Act
        var result = await helper.TwoFactorSignInAsync(provider, code, false, false);

        // Assert
        Assert.Same(expectedSignInResult, result);
        manager.Verify();
        auth.Verify();
    }

    private static SignInManager<PocoUser> SetupSignInManagerType(UserManager<PocoUser> manager, HttpContext context, string typeName)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(context);
        var roleManager = MockHelpers.MockRoleManager<PocoRole>();
        var options = Options.Create(new IdentityOptions());
        var claimsFactory = new UserClaimsPrincipalFactory<PocoUser, PocoRole>(manager, roleManager.Object, options);

        return typeName switch
        {
            nameof(SignInManager<PocoUser>) => new SignInManager<PocoUser>(manager, contextAccessor.Object, claimsFactory, options, NullLogger<SignInManager<PocoUser>>.Instance, Mock.Of<IAuthenticationSchemeProvider>(), new DefaultUserConfirmation<PocoUser>()),
            nameof(NoOverridesSignInManager<PocoUser>) => new NoOverridesSignInManager<PocoUser>(manager, contextAccessor.Object, claimsFactory, options),
            nameof(OverrideAndAwaitBaseResetSignInManager<PocoUser>) => new OverrideAndAwaitBaseResetSignInManager<PocoUser>(manager, contextAccessor.Object, claimsFactory, options),
            nameof(OverrideAndPassThroughUserManagerResetSignInManager<PocoUser>) => new OverrideAndPassThroughUserManagerResetSignInManager<PocoUser>(manager, contextAccessor.Object, claimsFactory, options),
            _ => throw new NotImplementedException(),
        };
    }

    private class NoOverridesSignInManager<TUser> : SignInManager<TUser> where TUser : class
    {
        public NoOverridesSignInManager(
            UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, NullLogger<SignInManager<TUser>>.Instance, Mock.Of<IAuthenticationSchemeProvider>(), new DefaultUserConfirmation<TUser>())
        {
        }
    }

    private class OverrideAndAwaitBaseResetSignInManager<TUser> : SignInManager<TUser> where TUser : class
    {
        public OverrideAndAwaitBaseResetSignInManager(
            UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, NullLogger<SignInManager<TUser>>.Instance, Mock.Of<IAuthenticationSchemeProvider>(), new DefaultUserConfirmation<TUser>())
        {
        }

        protected override async Task ResetLockout(TUser user)
        {
            await base.ResetLockout(user);
        }
    }

    private class OverrideAndPassThroughUserManagerResetSignInManager<TUser> : SignInManager<TUser> where TUser : class
    {
        public OverrideAndPassThroughUserManagerResetSignInManager(
            UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, NullLogger<SignInManager<TUser>>.Instance, Mock.Of<IAuthenticationSchemeProvider>(), new DefaultUserConfirmation<TUser>())
        {
        }

        protected override Task ResetLockout(TUser user)
        {
            if (UserManager.SupportsUserLockout)
            {
                return UserManager.ResetAccessFailedCountAsync(user);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MockSchemeProvider : IAuthenticationSchemeProvider
    {
        private static AuthenticationScheme CreateCookieScheme(string name) => new(IdentityConstants.ApplicationScheme, displayName: null, typeof(CookieAuthenticationHandler));

        private static readonly Dictionary<string, AuthenticationScheme> _defaultCookieSchemes = new()
        {
            [IdentityConstants.ApplicationScheme] = CreateCookieScheme(IdentityConstants.ApplicationScheme),
            [IdentityConstants.ExternalScheme] = CreateCookieScheme(IdentityConstants.ExternalScheme),
            [IdentityConstants.TwoFactorRememberMeScheme] = CreateCookieScheme(IdentityConstants.TwoFactorRememberMeScheme),
            [IdentityConstants.TwoFactorUserIdScheme] = CreateCookieScheme(IdentityConstants.TwoFactorUserIdScheme),
        };

        public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync() => Task.FromResult<IEnumerable<AuthenticationScheme>>(_defaultCookieSchemes.Values);
        public Task<AuthenticationScheme> GetSchemeAsync(string name) => Task.FromResult(_defaultCookieSchemes.TryGetValue(name, out var scheme) ? scheme : null);

        public void AddScheme(AuthenticationScheme scheme) => throw new NotImplementedException();
        public void RemoveScheme(string name) => throw new NotImplementedException();
        public Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync() => throw new NotImplementedException();
        public Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync() => throw new NotImplementedException();
        public Task<AuthenticationScheme> GetDefaultForbidSchemeAsync() => throw new NotImplementedException();
        public Task<AuthenticationScheme> GetDefaultSignInSchemeAsync() => throw new NotImplementedException();
        public Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync() => throw new NotImplementedException();
        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync() => throw new NotImplementedException();
    }
}
