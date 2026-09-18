// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Claims;
using AngleSharp.Html.Dom;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public abstract class ManagementTests<TStartup, TContext> : IClassFixture<ServerFactory<TStartup, TContext>>
    where TStartup : class
    where TContext : DbContext
{
    public ManagementTests(ServerFactory<TStartup, TContext> serverFactory)
    {
        ServerFactory = serverFactory;
    }

    public ServerFactory<TStartup, TContext> ServerFactory { get; }

    [Fact]
    public async Task CanEnableTwoFactorAuthentication()
    {
        // Arrange
        var client = ServerFactory
            .CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";

        var index = await UserStories.RegisterNewUserAsync(client, userName, password);

        // Act & Assert
        Assert.NotNull(await UserStories.EnableTwoFactorAuthentication(index));
    }

    [Fact]
    public async Task CannotEnableTwoFactorAuthenticationWithoutCookieConsent()
    {
        // Arrange
        var client = ServerFactory
            .CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";

        var index = await UserStories.RegisterNewUserAsync(client, userName, password);

        // Act & Assert
        Assert.Null(await UserStories.EnableTwoFactorAuthentication(index, consent: false));
    }

    [Fact]
    public async Task CanConfirmEmail()
    {
        // Arrange
        var emails = new ContosoEmailSender();
        void ConfigureTestServices(IServiceCollection services) =>
            services.SetupTestEmailSender(emails);

        var server = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));
        var client = server.CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";

        var index = await UserStories.RegisterNewUserAsync(client, userName, password);
        var manageIndex = await UserStories.SendEmailConfirmationLinkAsync(index);

        // Act & Assert
        Assert.Equal(2, emails.SentEmails.Count);
        var email = emails.SentEmails[1];
        await UserStories.ConfirmEmailAsync(email, client);
    }

    [Fact]
    public async Task CanChangeEmail()
    {
        // Arrange
        var emails = new ContosoEmailSender();
        void ConfigureTestServices(IServiceCollection services) =>
            services.SetupTestEmailSender(emails);

        var server = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));
        var client = server.CreateClient();
        var newClient = server.CreateClient();
        var failedClient = server.CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        var newEmail = "updatedEmail@example.com";

        var index = await UserStories.RegisterNewUserAsync(client, userName, password);
        var email = await UserStories.SendUpdateEmailAsync(index, newEmail);

        // Act & Assert
        Assert.Equal(2, emails.SentEmails.Count);
        await UserStories.ConfirmEmailAsync(emails.SentEmails[1], client);

        // Verify can login with new email, fails with old
        await UserStories.LoginExistingUserAsync(newClient, newEmail, password);
        await UserStories.LoginFailsAsync(failedClient, userName, password);

    }

    [Fact]
    public async Task CanChangePassword()
    {
        // Arrange
        var principals = new List<ClaimsPrincipal>();
        void ConfigureTestServices(IServiceCollection services) =>
            services.SetupGetUserClaimsPrincipal(user => principals.Add(user), IdentityConstants.ApplicationScheme);

        var server = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureTestServices(ConfigureTestServices));

        var client = server.CreateClient();
        var newClient = server.CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = "[PLACEHOLDER]-1a";
        var newPassword = "[PLACEHOLDER]-1a-updated";

        var index = await UserStories.RegisterNewUserAsync(client, userName, password);

        // Act 1
        var changedPassword = await UserStories.ChangePasswordAsync(index, password, newPassword);

        // Assert 1
        // RefreshSignIn generates a new security stamp claim
        AssertClaimsNotEqual(principals[0], principals[1], "AspNet.Identity.SecurityStamp");

        // Act 2
        await UserStories.LoginExistingUserAsync(newClient, userName, newPassword);

        // Assert 2
        // Signing in again with a different client uses the same security stamp claim
        AssertClaimsEqual(principals[1], principals[2], "AspNet.Identity.SecurityStamp");
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("DifferentUser")]
    [InlineData("Malformed")]
    [InlineData("ChangedStamp")]
    [InlineData("Expired")]
    public async Task CannotSetPasswordWithInvalidReauthentication(string marker)
    {
        var principals = new List<ClaimsPrincipal>();
        using var server = ServerFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.SetupTestThirdPartyLogin()
                .SetupGetUserClaimsPrincipal(user => principals.Add(user), IdentityConstants.ApplicationScheme)));
        var cookies = new CookieContainer();
        using var client = server.CreateClient(cookies);
        var userName = Guid.NewGuid().ToString();
        var email = $"{userName}@example.com";
        var password = "[PLACEHOLDER]-1a-updated";
        var index = await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        var manage = await index.ClickManageLinkAsync();
        var setPassword = await manage.ClickChangePasswordLinkExternalLoginAsync();

        switch (marker)
        {
            case "Missing":
                Assert.Null(cookies.GetCookies(client.BaseAddress)[SetPassword.ReauthenticationCookieName]);
                break;
            case "DifferentUser":
                var otherCookies = new CookieContainer();
                using (var otherClient = server.CreateClient(otherCookies))
                {
                    var otherUserName = Guid.NewGuid().ToString();
                    var otherIndex = await UserStories.RegisterNewUserWithSocialLoginAsync(otherClient, otherUserName, $"{otherUserName}@example.com");
                    var otherManage = await otherIndex.ClickManageLinkAsync();
                    var otherSetPassword = await otherManage.ClickChangePasswordLinkExternalLoginAsync();
                    await otherSetPassword.ReauthenticateAsync(otherUserName);
                    var otherMarker = Assert.IsType<Cookie>(otherCookies.GetCookies(otherClient.BaseAddress)[SetPassword.ReauthenticationCookieName]);
                    cookies.Add(client.BaseAddress, new Cookie(SetPassword.ReauthenticationCookieName, otherMarker.Value, "/"));
                }
                break;
            case "Malformed":
                cookies.Add(client.BaseAddress, new Cookie(SetPassword.ReauthenticationCookieName, "not-a-protected-marker", "/"));
                break;
            case "ChangedStamp":
                var beforeConfirmation = DateTimeOffset.UtcNow;
                setPassword = await setPassword.ReauthenticateAsync(userName);
                var afterConfirmation = DateTimeOffset.UtcNow;
                var issuedMarker = Assert.IsType<Cookie>(cookies.GetCookies(client.BaseAddress)[SetPassword.ReauthenticationCookieName]);
                using (var scope = server.Services.CreateScope())
                {
                    var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                        .CreateProtector("Microsoft.AspNetCore.Identity.UI.ReauthenticationMarker.v1")
                        .ToTimeLimitedDataProtector();
                    protector.Unprotect(issuedMarker.Value, out var expiration);
                    Assert.InRange(expiration, beforeConfirmation.AddMinutes(5), afterConfirmation.AddMinutes(5));
                    // UserManager.AddPasswordAsync rotates the security stamp while this marker is still live.
                    await setPassword.SetPasswordAsync("[PLACEHOLDER]-1a-original");
                    protector.Unprotect(issuedMarker.Value, out _);
                    cookies.Add(client.BaseAddress, new Cookie(SetPassword.ReauthenticationCookieName, issuedMarker.Value, "/"));
                }
                break;
            case "Expired":
                using (var scope = server.Services.CreateScope())
                {
                    var claimTypes = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value.ClaimsIdentity;
                    var principal = Assert.Single(principals);
                    var userId = Assert.Single(principal.FindAll(claimTypes.UserIdClaimType)).Value;
                    var stamp = Assert.Single(principal.FindAll(claimTypes.SecurityStampClaimType)).Value;
                    var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                        .CreateProtector("Microsoft.AspNetCore.Identity.UI.ReauthenticationMarker.v1")
                        .ToTimeLimitedDataProtector();
                    // This is verifier-level expiry coverage, not natural aging of a callback-issued marker.
                    var expiredMarker = protector.Protect($"{userId}:{stamp}", DateTimeOffset.UtcNow.AddMinutes(-1));
                    cookies.Add(client.BaseAddress, new Cookie(SetPassword.ReauthenticationCookieName, expiredMarker, "/"));
                }
                break;
        }

        var response = await setPassword.PostPasswordAsync(password);
        var content = await response.Content.ReadAsStringAsync();
        using var loginClient = server.CreateClient();
        var loginFailure = await Record.ExceptionAsync(() => UserStories.LoginFailsAsync(loginClient, email, password));

        Assert.Multiple(
            () => ResponseAssert.IsOK(response),
            () => Assert.Contains(SetPassword.ReauthenticationRefusal, content),
            () => Assert.Null(loginFailure));

        if (marker == "ChangedStamp")
        {
            using var originalPasswordClient = server.CreateClient();
            await UserStories.LoginExistingUserAsync(originalPasswordClient, email, "[PLACEHOLDER]-1a-original");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CannotSetPasswordAfterReauthenticationWithUnlinkedAccount(bool linkedToAnotherUser)
    {
        using var server = ServerFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.SetupTestThirdPartyLogin()));
        var cookies = new CookieContainer();
        using var client = server.CreateClient(cookies);
        var userName = Guid.NewGuid().ToString();
        var email = $"{userName}@example.com";
        var password = "[PLACEHOLDER]-1a-updated";
        var index = await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        var manage = await index.ClickManageLinkAsync();
        var setPassword = await manage.ClickChangePasswordLinkExternalLoginAsync();
        var otherUserName = Guid.NewGuid().ToString();
        if (linkedToAnotherUser)
        {
            using var otherClient = server.CreateClient();
            await UserStories.RegisterNewUserWithSocialLoginAsync(otherClient, otherUserName, $"{otherUserName}@example.com");
        }

        setPassword = await setPassword.ReauthenticateAsync(otherUserName);

        Assert.Contains("Error: That login is not linked to this account.", setPassword.Document.Body.TextContent);
        Assert.Null(cookies.GetCookies(client.BaseAddress)[SetPassword.ReauthenticationCookieName]);
        var response = await setPassword.PostPasswordAsync(password);
        ResponseAssert.IsOK(response);
        Assert.Contains(SetPassword.ReauthenticationRefusal, await response.Content.ReadAsStringAsync());
        using var loginClient = server.CreateClient();
        await UserStories.LoginFailsAsync(loginClient, email, password);
    }

    [Theory]
    [InlineData("NotRegistered")]
    [InlineData(null)]
    [InlineData("Other")]
    [InlineData("Contoso")]
    public async Task CannotReauthenticateSetPasswordWithUnavailableProvider(string provider)
    {
        using var server = ServerFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.SetupTestThirdPartyLogin().AddAuthentication()
                    .AddScheme<ContosoAuthenticationOptions, ContosoAuthenticationHandler>("Other", "Other", options =>
                        options.SignInScheme = IdentityConstants.ExternalScheme);
            }));
        using var client = server.CreateClient();
        var userName = Guid.NewGuid().ToString();
        var email = $"{userName}@example.com";
        var password = "[PLACEHOLDER]-1a-updated";
        var index = await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        if (provider == "Contoso")
        {
            server.Services.GetRequiredService<IAuthenticationSchemeProvider>().RemoveScheme("Contoso");
        }

        var manage = await index.ClickManageLinkAsync();
        var setPassword = await manage.ClickChangePasswordLinkExternalLoginAsync();
        var response = await setPassword.PostReauthenticationAsync(provider);

        ResponseAssert.IsOK(response);
        Assert.Contains("The selected external login is not available for confirmation.", await response.Content.ReadAsStringAsync());
        var document = await ResponseAssert.IsHtmlDocumentAsync(response);
        HtmlAssert.HasForm("#set-password-form", document);
        Assert.Null(document.QuerySelector("#Input_NewPassword"));
        var buttons = document.QuerySelectorAll("#reauthenticate-form button");
        const string noLoginsMessage = "You must have a configured external login linked to this account to confirm your identity before setting a password.";
        if (provider == "Contoso")
        {
            Assert.Empty(buttons);
            Assert.Contains(noLoginsMessage, setPassword.Document.Body.TextContent);
            Assert.Contains(noLoginsMessage, document.Body.TextContent);
        }
        else
        {
            Assert.Equal("Contoso", Assert.Single(buttons).GetAttribute("value"));
            Assert.DoesNotContain(noLoginsMessage, setPassword.Document.Body.TextContent);
            Assert.DoesNotContain(noLoginsMessage, document.Body.TextContent);
        }

        setPassword = new SetPassword(client, document, setPassword.Context);
        var passwordResponse = await setPassword.PostPasswordAsync(password);
        ResponseAssert.IsOK(passwordResponse);
        Assert.Contains(SetPassword.ReauthenticationRefusal, await passwordResponse.Content.ReadAsStringAsync());
        using var loginClient = server.CreateClient();
        await UserStories.LoginFailsAsync(loginClient, email, password);
    }

    [Theory]
    [InlineData("[PLACEHOLDER]-1a-updated", "different", "The new password and confirmation password do not match.")]
    [InlineData("alllowercase", "alllowercase", "Passwords must have at least one digit ('0'-'9').")]
    [InlineData("[PLACEHOLDER]-1a-final", null, "The selected external login is not available for confirmation.")]
    public async Task CanSetPasswordAfterValidationFailure(string password, string confirmation, string error)
    {
        using var server = ServerFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.SetupTestThirdPartyLogin()));
        var cookies = new CookieContainer();
        using var client = server.CreateClient(cookies);
        var userName = Guid.NewGuid().ToString();
        var email = $"{userName}@example.com";
        var index = await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        var manage = await index.ClickManageLinkAsync();
        var setPassword = await manage.ClickChangePasswordLinkExternalLoginAsync();
        Assert.Null(setPassword.Document.QuerySelector("#Input_NewPassword"));
        setPassword = await setPassword.ReauthenticateAsync(userName);
        var issuedMarker = Assert.IsType<Cookie>(cookies.GetCookies(client.BaseAddress)[SetPassword.ReauthenticationCookieName]);
        Assert.True(issuedMarker.HttpOnly);
        Assert.True(issuedMarker.Secure);
        Assert.Equal("/", issuedMarker.Path);
        var marker = issuedMarker.Value;

        var response = confirmation is null
            ? await setPassword.PostReauthenticationAsync("NotRegistered")
            : await setPassword.PostPasswordAsync(password, confirmation);
        var document = await ResponseAssert.IsHtmlDocumentAsync(response);

        Assert.Contains(error, document.Body.TextContent);
        var form = HtmlAssert.HasForm("#set-password-form", document);
        Assert.IsAssignableFrom<IHtmlInputElement>(form["Input_NewPassword"]);
        Assert.IsAssignableFrom<IHtmlInputElement>(form["Input_ConfirmPassword"]);
        HtmlAssert.HasElement("button[type=submit]", form);
        Assert.Null(document.QuerySelector("#reauthenticate-form"));
        Assert.Equal(marker, cookies.GetCookies(client.BaseAddress)[SetPassword.ReauthenticationCookieName].Value);
        Assert.DoesNotContain(response.Headers.Where(header => header.Key == "Set-Cookie").SelectMany(header => header.Value),
            value => value.StartsWith(SetPassword.ReauthenticationCookieName + "=", StringComparison.Ordinal));
        using var failedLoginClient = server.CreateClient();
        await UserStories.LoginFailsAsync(failedLoginClient, email, password);

        setPassword = new SetPassword(client, document, setPassword.Context);
        await setPassword.SetPasswordAsync("[PLACEHOLDER]-1a-final");
        using var loginClient = server.CreateClient();
        await UserStories.LoginExistingUserAsync(loginClient, email, "[PLACEHOLDER]-1a-final");
    }

    [Fact]
    public async Task CanSetPasswordWithExternalLogin()
    {
        // Arrange
        var principals = new List<ClaimsPrincipal>();
        void ConfigureTestServices(IServiceCollection services) =>
            services
                .SetupTestThirdPartyLogin()
                .SetupGetUserClaimsPrincipal(user => principals.Add(user), IdentityConstants.ApplicationScheme);

        var server = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureTestServices(ConfigureTestServices));

        var client = server.CreateClient();
        var newClient = server.CreateClient();
        var loginAfterSetPasswordClient = server.CreateClient();

        var guid = Guid.NewGuid();
        var userName = $"{guid}";
        var email = $"{guid}@example.com";

        // Act 1
        var index = await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        index = await UserStories.LoginWithSocialLoginAsync(newClient, userName);

        // Assert 1
        Assert.NotNull(principals[1].Identities.Single().Claims.Single(c => c.Type == ClaimTypes.AuthenticationMethod).Value);

        // Act 2
        await UserStories.SetPasswordAsync(index, "[PLACEHOLDER]-1a-updated", userName);

        // Assert 2
        // RefreshSignIn uses the same AuthenticationMethod claim value
        AssertClaimsEqual(principals[1], principals[2], ClaimTypes.AuthenticationMethod);

        // Act & Assert 3
        // Can log in with the password set above
        await UserStories.LoginExistingUserAsync(loginAfterSetPasswordClient, email, "[PLACEHOLDER]-1a-updated");
    }

    [Fact]
    public async Task CanRemoveExternalLogin()
    {
        // Arrange
        var principals = new List<ClaimsPrincipal>();
        void ConfigureTestServices(IServiceCollection services) =>
            services
                .SetupTestThirdPartyLogin()
                .SetupGetUserClaimsPrincipal(user => principals.Add(user), IdentityConstants.ApplicationScheme);

        var server = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureTestServices(ConfigureTestServices));

        var client = server.CreateClient();

        var guid = Guid.NewGuid();
        var userName = $"{guid}";
        var email = $"{guid}@example.com";

        // Act
        var index = await UserStories.RegisterNewUserAsync(client, email, "[PLACEHOLDER]-1a");
        var linkLogin = await UserStories.LinkExternalLoginAsync(index, email);
        await UserStories.RemoveExternalLoginAsync(linkLogin, email);

        // RefreshSignIn generates a new security stamp claim
        AssertClaimsNotEqual(principals[0], principals[1], "AspNet.Identity.SecurityStamp");
    }

    [Fact]
    public async Task CanSeeExternalLoginProviderDisplayName()
    {
        // Arrange
        void ConfigureTestServices(IServiceCollection services) => services.SetupTestThirdPartyLogin();

        var server = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureTestServices(ConfigureTestServices));

        var client = server.CreateClient();

        // Act
        var userName = Guid.NewGuid().ToString();
        var email = $"{userName}@example.com";
        var index = await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        var manage = await index.ClickManageLinkWithExternalLoginAsync();
        var externalLogins = await manage.ClickExternalLoginsAsync();

        // Assert
        Assert.Contains("Contoso", externalLogins.ExternalLoginDisplayName.TextContent);
    }

    [Fact]
    public async Task CanResetAuthenticator()
    {
        // Arrange
        var principals = new List<ClaimsPrincipal>();
        void ConfigureTestServices(IServiceCollection services) =>
            services
                .SetupTestThirdPartyLogin()
                .SetupGetUserClaimsPrincipal(user => principals.Add(user), IdentityConstants.ApplicationScheme);

        var server = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureTestServices(ConfigureTestServices));

        var client = server.CreateClient();
        var newClient = server.CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";

        // Act
        var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);
        var showRecoveryCodes = await UserStories.EnableTwoFactorAuthentication(loggedIn);
        var twoFactorKey = showRecoveryCodes.Context.AuthenticatorKey;

        // Use a new client to simulate a new browser session.
        await UserStories.AcceptCookiePolicy(newClient);
        var index = await UserStories.LoginExistingUser2FaAsync(newClient, userName, password, twoFactorKey);
        await UserStories.ResetAuthenticator(index);

        // RefreshSignIn generates a new security stamp claim
        AssertClaimsNotEqual(principals[1], principals[2], "AspNet.Identity.SecurityStamp");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CanDownloadPersonalData(bool twoFactor, bool social)
    {
        // Arrange
        void ConfigureTestServices(IServiceCollection services) =>
            services.SetupTestThirdPartyLogin();

        var client = ServerFactory
            .WithWebHostBuilder(whb => whb.ConfigureTestServices(ConfigureTestServices))
            .CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var guid = Guid.NewGuid();
        var email = userName;

        var index = social
            ? await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email)
            : await UserStories.RegisterNewUserAsync(client, email, "[PLACEHOLDER]-1a");

        if (twoFactor)
        {
            await UserStories.EnableTwoFactorAuthentication(index);
        }

        // Act & Assert
        var jsonData = await UserStories.DownloadPersonalData(index, userName);
        Assert.NotNull(jsonData);
        Assert.True(jsonData.ContainsKey("Id"));
        Assert.NotNull(jsonData["Id"]);
        Assert.True(jsonData.ContainsKey("UserName"));
        Assert.Equal(userName, (string)jsonData["UserName"]);
        Assert.True(jsonData.ContainsKey("Email"));
        Assert.Equal(userName, (string)jsonData["Email"]);
        Assert.True(jsonData.ContainsKey("EmailConfirmed"));
        Assert.False((bool)jsonData["EmailConfirmed"]);
        Assert.True(jsonData.ContainsKey("PhoneNumber"));
        Assert.Equal("null", (string)jsonData["PhoneNumber"]);
        Assert.True(jsonData.ContainsKey("PhoneNumberConfirmed"));
        Assert.False((bool)jsonData["PhoneNumberConfirmed"]);
        Assert.Equal(twoFactor, (bool)jsonData["TwoFactorEnabled"]);

        if (twoFactor)
        {
            Assert.NotNull(jsonData["Authenticator Key"]);
        }
        else
        {
            Assert.Null((string)jsonData["Authenticator Key"]);
        }

        if (social)
        {
            Assert.Equal(userName, (string)jsonData["Contoso external login provider key"]);
        }
        else
        {
            Assert.Null((string)jsonData["Contoso external login provider key"]);
        }
    }

    [Fact]
    public async Task GetOnDownloadPersonalData_ReturnsNotFound()
    {
        // Arrange
        var client = ServerFactory
            .CreateClient();

        await UserStories.RegisterNewUserAsync(client);

        // Act
        var response = await client.GetAsync("/Identity/Account/Manage/DownloadPersonalData");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CanDeleteUser()
    {
        // Arrange
        var client = ServerFactory
            .CreateClient();

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";

        var index = await UserStories.RegisterNewUserAsync(client, userName, password);

        // Act & Assert
        await UserStories.DeleteUser(index, password);
    }

    private void AssertClaimsEqual(ClaimsPrincipal expectedPrincipal, ClaimsPrincipal actualPrincipal, string claimType)
    {
        var expectedPrincipalClaim = expectedPrincipal.Identities.Single().Claims.Single(c => c.Type == claimType).Value;
        var actualPrincipalClaim = actualPrincipal.Identities.Single().Claims.Single(c => c.Type == claimType).Value;
        Assert.Equal(expectedPrincipalClaim, actualPrincipalClaim);
    }

    private void AssertClaimsNotEqual(ClaimsPrincipal expectedPrincipal, ClaimsPrincipal actualPrincipal, string claimType)
    {
        var expectedPrincipalClaim = expectedPrincipal.Identities.Single().Claims.Single(c => c.Type == claimType).Value;
        var actualPrincipalClaim = actualPrincipal.Identities.Single().Claims.Single(c => c.Type == claimType).Value;
        Assert.NotEqual(expectedPrincipalClaim, actualPrincipalClaim);
    }
}
