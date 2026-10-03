// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests;

public class IdentityUserManagementTests : ManagementTests<Startup, IdentityDbContext>
{
    public IdentityUserManagementTests(ServerFactory<Startup, IdentityDbContext> serverFactory) : base(serverFactory)
    {
    }

    [Theory]
    [InlineData("V4")]
    [InlineData("V5")]
    public async Task CannotSetPasswordWithoutSecurityStampSupport(string framework)
    {
        var useUnsupportedStore = false;
        using var factory = new ServerFactory<Startup, IdentityDbContext> { BootstrapFrameworkVersion = framework };
        using var server = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.SetupTestThirdPartyLogin();
            var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IUserStore<IdentityUser>));
            services.Remove(descriptor);
            services.Add(new ServiceDescriptor(descriptor.ServiceType, provider =>
            {
                var store = Assert.IsAssignableFrom<IUserStore<IdentityUser>>(
                    descriptor.ImplementationInstance ?? descriptor.ImplementationFactory?.Invoke(provider) ??
                    ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType));
                if (!useUnsupportedStore)
                {
                    return store;
                }

                var unsupportedStore = new Mock<IUserStore<IdentityUser>>(MockBehavior.Strict);
                unsupportedStore.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns<string, CancellationToken>(store.FindByIdAsync);
                unsupportedStore.Setup(s => s.GetUserIdAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                    .Returns<IdentityUser, CancellationToken>(store.GetUserIdAsync);
                unsupportedStore.As<IUserPasswordStore<IdentityUser>>()
                    .Setup(s => s.HasPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                    .Returns<IdentityUser, CancellationToken>(Assert.IsAssignableFrom<IUserPasswordStore<IdentityUser>>(store).HasPasswordAsync);
                unsupportedStore.Setup(s => s.Dispose()).Callback(store.Dispose);
                return unsupportedStore.Object;
            }, descriptor.Lifetime));
        }));
        using var client = server.CreateClient();
        var userName = Guid.NewGuid().ToString();
        var email = $"{userName}@example.com";
        var password = "[PLACEHOLDER]-1a-updated";
        var index = await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        var manage = await index.ClickManageLinkAsync();

        useUnsupportedStore = true;
        using (var scope = server.Services.CreateScope())
        {
            Assert.False(scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().SupportsUserSecurityStamp);
        }

        var setPassword = await manage.ClickChangePasswordLinkExternalLoginAsync();
        var response = await setPassword.PostPasswordAsync(password);
        var document = await ResponseAssert.IsHtmlDocumentAsync(response);
        var summary = HtmlAssert.HasElement("#set-password-form .validation-summary-errors", document).TextContent;

        useUnsupportedStore = false;
        using var loginClient = server.CreateClient();
        await UserStories.LoginFailsAsync(loginClient, email, password);

        Assert.Multiple(
            () => Assert.Contains("Setting a password requires a user store that supports security stamps.", summary),
            () => Assert.DoesNotContain(SetPassword.ReauthenticationRefusal, summary));
    }
}
