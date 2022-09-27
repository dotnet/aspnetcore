// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class UserManagerTest
{
    [Fact]
    public void EnsureDefaultServicesDefaultsWithStoreWorks()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddTransient<IUserStore<PocoUser>, NoopUserStore>();
        services.AddIdentity<PocoUser, PocoRole>();
        services.AddHttpContextAccessor();
        services.AddLogging();
        var manager = services.BuildServiceProvider().GetRequiredService<UserManager<PocoUser>>();
        Assert.NotNull(manager.PasswordHasher);
        Assert.NotNull(manager.Options);
    }

    [Fact]
    public void AddUserManagerWithCustomManagerReturnsSameInstance()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddTransient<IUserStore<PocoUser>, NoopUserStore>()
                .AddHttpContextAccessor();

        services.AddLogging();

        services.AddIdentity<PocoUser, PocoRole>()
            .AddUserManager<CustomUserManager>()
            .AddRoleManager<CustomRoleManager>();
        var provider = services.BuildServiceProvider();
        Assert.Same(provider.GetRequiredService<UserManager<PocoUser>>(),
            provider.GetRequiredService<CustomUserManager>());
        Assert.Same(provider.GetRequiredService<RoleManager<PocoRole>>(),
            provider.GetRequiredService<CustomRoleManager>());
    }

    public class CustomUserManager : UserManager<PocoUser>
    {
        public CustomUserManager() : base(new Mock<IUserStore<PocoUser>>().Object, null, null, null, null, null, null, null, null)
        { }
    }

    public class CustomRoleManager : RoleManager<PocoRole>
    {
        public CustomRoleManager() : base(new Mock<IRoleStore<PocoRole>>().Object, null, null, null, null)
        { }
    }

    [Fact]
    public async Task CreateCallsStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        store.Setup(s => s.CreateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
        store.Setup(s => s.SetNormalizedUserNameAsync(user, normalizer.NormalizeName(user.UserName), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.CreateAsync(user);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task CreateUpdatesSecurityStampStore()
    {
        // Setup
        var store = new Mock<IUserSecurityStampStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo", SecurityStamp = "sssss" };
        store.Setup(s => s.CreateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.GetSecurityStampAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.SecurityStamp)).Verifiable();
        store.Setup(s => s.SetSecurityStampAsync(user, It.IsAny<string>(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.CreateAsync(user);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task CreateCallsUpdateEmailStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserEmailStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo", Email = "Foo@foo.com" };
        store.Setup(s => s.CreateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
        store.Setup(s => s.GetEmailAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.Email)).Verifiable();
        store.Setup(s => s.SetNormalizedEmailAsync(user, normalizer.NormalizeEmail(user.Email), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        store.Setup(s => s.SetNormalizedUserNameAsync(user, normalizer.NormalizeName(user.UserName), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.CreateAsync(user);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task DeleteCallsStore()
    {
        // Setup
        var store = new Mock<IUserStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        store.Setup(s => s.DeleteAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.DeleteAsync(user);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task UpdateCallsStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
        store.Setup(s => s.SetNormalizedUserNameAsync(user, normalizer.NormalizeName(user.UserName), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.UpdateAsync(user);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task UpdateWillUpdateNormalizedEmail()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserEmailStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo", Email = "email" };
        store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
        store.Setup(s => s.GetEmailAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.Email)).Verifiable();
        store.Setup(s => s.SetNormalizedUserNameAsync(user, normalizer.NormalizeName(user.UserName), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        store.Setup(s => s.SetNormalizedEmailAsync(user, normalizer.NormalizeEmail(user.Email), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.UpdateAsync(user);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task SetUserNameCallsStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserStore<PocoUser>>();
        var user = new PocoUser();
        store.Setup(s => s.SetUserNameAsync(user, "foo", CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult("foo")).Verifiable();
        store.Setup(s => s.SetNormalizedUserNameAsync(user, normalizer.NormalizeName("foo"), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.SetUserNameAsync(user, "foo");

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task FindByIdCallsStore()
    {
        // Setup
        var store = new Mock<IUserStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        store.Setup(s => s.FindByIdAsync(user.Id, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.FindByIdAsync(user.Id);

        // Assert
        Assert.Equal(user, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task FindByNameCallsStoreWithNormalizedName()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        store.Setup(s => s.FindByNameAsync(normalizer.NormalizeName(user.UserName), CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.FindByNameAsync(user.UserName);

        // Assert
        Assert.Equal(user, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task CanFindByNameCallsStoreWithoutNormalizedName()
    {
        // Setup
        var store = new Mock<IUserStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        store.Setup(s => s.FindByNameAsync(user.UserName, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);
        userManager.KeyNormalizer = null;

        // Act
        var result = await userManager.FindByNameAsync(user.UserName);

        // Assert
        Assert.Equal(user, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task FindByEmailCallsStoreWithNormalizedEmail()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserEmailStore<PocoUser>>();
        var user = new PocoUser { Email = "Foo" };
        store.Setup(s => s.FindByEmailAsync(normalizer.NormalizeEmail(user.Email), CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.FindByEmailAsync(user.Email);

        // Assert
        Assert.Equal(user, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task CanFindByEmailCallsStoreWithoutNormalizedEmail()
    {
        // Setup
        var store = new Mock<IUserEmailStore<PocoUser>>();
        var user = new PocoUser { Email = "Foo" };
        store.Setup(s => s.FindByEmailAsync(user.Email, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);
        userManager.KeyNormalizer = null;

        // Act
        var result = await userManager.FindByEmailAsync(user.Email);

        // Assert
        Assert.Equal(user, result);
        store.VerifyAll();
    }

    private class CustomNormalizer : ILookupNormalizer
    {
        public string NormalizeName(string key) => "#" + key;
        public string NormalizeEmail(string key) => "@" + key;
    }

    [Fact]
    public async Task FindByEmailCallsStoreWithCustomNormalizedEmail()
    {
        // Setup
        var store = new Mock<IUserEmailStore<PocoUser>>();
        var user = new PocoUser { Email = "Foo" };
        store.Setup(s => s.FindByEmailAsync("@Foo", CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);
        userManager.KeyNormalizer = new CustomNormalizer();

        // Act
        var result = await userManager.FindByEmailAsync(user.Email);

        // Assert
        Assert.Equal(user, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task FindByNameCallsStoreWithCustomNormalizedName()
    {
        // Setup
        var store = new Mock<IUserEmailStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo", Email = "Bar" };
        store.Setup(s => s.FindByNameAsync("#Foo", CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);
        userManager.KeyNormalizer = new CustomNormalizer();

        // Act
        var result = await userManager.FindByNameAsync(user.UserName);

        // Assert
        Assert.Equal(user, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task AddToRolesCallsStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserRoleStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var roles = new string[] { "A", "B", "C", "C" };
        store.Setup(s => s.AddToRoleAsync(user, normalizer.NormalizeName("A"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.AddToRoleAsync(user, normalizer.NormalizeName("B"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.AddToRoleAsync(user, normalizer.NormalizeName("C"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();

        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("A"), CancellationToken.None))
            .Returns(Task.FromResult(false))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("B"), CancellationToken.None))
            .Returns(Task.FromResult(false))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("C"), CancellationToken.None))
            .Returns(Task.FromResult(false))
            .Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.AddToRolesAsync(user, roles);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
        store.Verify(s => s.AddToRoleAsync(user, normalizer.NormalizeName("C"), CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task AddToRolesCallsStoreWithCustomNameNormalizer()
    {
        // Setup
        var store = new Mock<IUserRoleStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var roles = new string[] { "A", "B", "C", "C" };
        store.Setup(s => s.AddToRoleAsync(user, "#A", CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.AddToRoleAsync(user, "#B", CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.AddToRoleAsync(user, "#C", CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();

        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, "#A", CancellationToken.None))
            .Returns(Task.FromResult(false))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, "#B", CancellationToken.None))
            .Returns(Task.FromResult(false))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, "#C", CancellationToken.None))
            .Returns(Task.FromResult(false))
            .Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);
        userManager.KeyNormalizer = new CustomNormalizer();

        // Act
        var result = await userManager.AddToRolesAsync(user, roles);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
        store.Verify(s => s.AddToRoleAsync(user, "#C", CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task AddToRolesFailsIfUserInRole()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserRoleStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var roles = new[] { "A", "B", "C" };
        store.Setup(s => s.AddToRoleAsync(user, normalizer.NormalizeName("A"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("B"), CancellationToken.None))
            .Returns(Task.FromResult(true))
            .Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.AddToRolesAsync(user, roles);

        // Assert
        IdentityResultAssert.IsFailure(result, new IdentityErrorDescriber().UserAlreadyInRole("B"));
        store.VerifyAll();
    }

    [Fact]
    public async Task RemoveFromRolesCallsStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserRoleStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var roles = new[] { "A", "B", "C" };
        store.Setup(s => s.RemoveFromRoleAsync(user, normalizer.NormalizeName("A"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.RemoveFromRoleAsync(user, normalizer.NormalizeName("B"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.RemoveFromRoleAsync(user, normalizer.NormalizeName("C"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("A"), CancellationToken.None))
            .Returns(Task.FromResult(true))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("B"), CancellationToken.None))
            .Returns(Task.FromResult(true))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("C"), CancellationToken.None))
            .Returns(Task.FromResult(true))
            .Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.RemoveFromRolesAsync(user, roles);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task RemoveFromRolesFailsIfNotInRole()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IUserRoleStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var roles = new string[] { "A", "B", "C" };
        store.Setup(s => s.RemoveFromRoleAsync(user, normalizer.NormalizeName("A"), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("A"), CancellationToken.None))
            .Returns(Task.FromResult(true))
            .Verifiable();
        store.Setup(s => s.IsInRoleAsync(user, normalizer.NormalizeName("B"), CancellationToken.None))
            .Returns(Task.FromResult(false))
            .Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.RemoveFromRolesAsync(user, roles);

        // Assert
        IdentityResultAssert.IsFailure(result, new IdentityErrorDescriber().UserNotInRole("B"));
        store.VerifyAll();
    }

    [Fact]
    public async Task AddClaimsCallsStore()
    {
        // Setup
        var store = new Mock<IUserClaimStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var claims = new Claim[] { new Claim("1", "1"), new Claim("2", "2"), new Claim("3", "3") };
        store.Setup(s => s.AddClaimsAsync(user, claims, CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.AddClaimsAsync(user, claims);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task AddClaimCallsStore()
    {
        // Setup
        var store = new Mock<IUserClaimStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var claim = new Claim("1", "1");
        store.Setup(s => s.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>(), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.AddClaimAsync(user, claim);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task UpdateClaimCallsStore()
    {
        // Setup
        var store = new Mock<IUserClaimStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var claim = new Claim("1", "1");
        var newClaim = new Claim("1", "2");
        store.Setup(s => s.ReplaceClaimAsync(user, It.IsAny<Claim>(), It.IsAny<Claim>(), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.ReplaceClaimAsync(user, claim, newClaim);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task CheckPasswordWillRehashPasswordWhenNeeded()
    {
        // Setup
        var store = new Mock<IUserPasswordStore<PocoUser>>();
        var hasher = new Mock<IPasswordHasher<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var pwd = "password";
        var hashed = "hashed";
        var rehashed = "rehashed";

        store.Setup(s => s.GetPasswordHashAsync(user, CancellationToken.None))
            .ReturnsAsync(hashed)
            .Verifiable();
        store.Setup(s => s.SetPasswordHashAsync(user, It.IsAny<string>(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        store.Setup(x => x.UpdateAsync(It.IsAny<PocoUser>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(IdentityResult.Success));

        hasher.Setup(s => s.VerifyHashedPassword(user, hashed, pwd)).Returns(PasswordVerificationResult.SuccessRehashNeeded).Verifiable();
        hasher.Setup(s => s.HashPassword(user, pwd)).Returns(rehashed).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);
        userManager.PasswordHasher = hasher.Object;

        // Act
        var result = await userManager.CheckPasswordAsync(user, pwd);

        // Assert
        Assert.True(result);
        store.VerifyAll();
        hasher.VerifyAll();
    }

    [Fact]
    public async Task CreateFailsWithNullSecurityStamp()
    {
        // Setup
        var store = new Mock<IUserSecurityStampStore<PocoUser>>();
        var manager = MockHelpers.TestUserManager(store.Object);
        var user = new PocoUser { UserName = "nulldude" };
        store.Setup(s => s.GetSecurityStampAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(default(string)).Verifiable();

        // Act
        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateAsync(user));
        Assert.Contains(Extensions.Identity.Core.Resources.NullSecurityStamp, ex.Message);

        store.VerifyAll();
    }

    [Fact]
    public async Task UpdateFailsWithNullSecurityStamp()
    {
        // Setup
        var store = new Mock<IUserSecurityStampStore<PocoUser>>();
        var manager = MockHelpers.TestUserManager(store.Object);
        var user = new PocoUser { UserName = "nulldude" };
        store.Setup(s => s.GetSecurityStampAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(default(string)).Verifiable();

        // Act
        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.UpdateAsync(user));
        Assert.Contains(Extensions.Identity.Core.Resources.NullSecurityStamp, ex.Message);

        store.VerifyAll();
    }

    [Fact]
    public async Task RemoveClaimsCallsStore()
    {
        // Setup
        var store = new Mock<IUserClaimStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var claims = new Claim[] { new Claim("1", "1"), new Claim("2", "2"), new Claim("3", "3") };
        store.Setup(s => s.RemoveClaimsAsync(user, claims, CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        var userManager = MockHelpers.TestUserManager(store.Object);

        // Act
        var result = await userManager.RemoveClaimsAsync(user, claims);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task RemoveClaimCallsStore()
    {
        // Setup
        var store = new Mock<IUserClaimStore<PocoUser>>();
        var user = new PocoUser { UserName = "Foo" };
        var claim = new Claim("1", "1");
        store.Setup(s => s.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>(), CancellationToken.None))
            .Returns(Task.FromResult(0))
            .Verifiable();
        store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        var userManager = MockHelpers.TestUserManager<PocoUser>(store.Object);

        // Act
        var result = await userManager.RemoveClaimAsync(user, claim);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task CheckPasswordWithNullUserReturnsFalse()
    {
        var manager = MockHelpers.TestUserManager(new EmptyStore());
        Assert.False(await manager.CheckPasswordAsync(null, "whatevs"));
    }

    [Fact]
    public void UsersQueryableFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsQueryableUsers);
        Assert.Throws<NotSupportedException>(() => manager.Users.Count());
    }

    [Fact]
    public async Task UsersEmailMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserEmail);
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.FindByEmailAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.SetEmailAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetEmailAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.IsEmailConfirmedAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.ConfirmEmailAsync(null, null));
    }

    [Fact]
    public async Task UsersPhoneNumberMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserPhoneNumber);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumberAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumberAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetPhoneNumberAsync(null));
    }

    [Fact]
    public async Task TokenMethodsThrowWithNoTokenProvider()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        var user = new PocoUser();
        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await manager.GenerateUserTokenAsync(user, "bogus", null));
        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await manager.VerifyUserTokenAsync(user, "bogus", null, null));
    }

    [Fact]
    public async Task PasswordMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserPassword);
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.CreateAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.ChangePasswordAsync(null, null, null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.AddPasswordAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.RemovePasswordAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.CheckPasswordAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.HasPasswordAsync(null));
    }

    [Fact]
    public async Task SecurityStampMethodsFailWhenStoreNotImplemented()
    {
        var store = new Mock<IUserStore<PocoUser>>();
        store.Setup(x => x.GetUserIdAsync(It.IsAny<PocoUser>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Guid.NewGuid().ToString()));
        var manager = MockHelpers.TestUserManager(store.Object);
        Assert.False(manager.SupportsUserSecurityStamp);
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.UpdateSecurityStampAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetSecurityStampAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(
                () => manager.VerifyChangePhoneNumberTokenAsync(new PocoUser(), "1", "111-111-1111"));
        await Assert.ThrowsAsync<NotSupportedException>(
                () => manager.GenerateChangePhoneNumberTokenAsync(new PocoUser(), "111-111-1111"));
    }

    [Fact]
    public async Task LoginMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserLogin);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddLoginAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveLoginAsync(null, null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLoginsAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.FindByLoginAsync(null, null));
    }

    [Fact]
    public async Task ClaimMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserClaim);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddClaimAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.ReplaceClaimAsync(null, null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveClaimAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetClaimsAsync(null));
    }

    private class ATokenProvider : IUserTwoFactorTokenProvider<PocoUser>
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<PocoUser> manager, PocoUser user)
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateAsync(string purpose, UserManager<PocoUser> manager, PocoUser user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<PocoUser> manager, PocoUser user)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void UserManagerWillUseTokenProviderInstance()
    {
        var provider = new ATokenProvider();
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddLogging();

        services.AddIdentity<PocoUser, PocoRole>(o => o.Tokens.ProviderMap.Add("A", new TokenProviderDescriptor(typeof(ATokenProvider))
        {
            ProviderInstance = provider
        })).AddUserStore<NoopUserStore>();
        var manager = services.BuildServiceProvider().GetService<UserManager<PocoUser>>();
        Assert.ThrowsAsync<NotImplementedException>(() => manager.GenerateUserTokenAsync(new PocoUser(), "A", "purpose"));
    }

    [Fact]
    public void UserManagerThrowsIfStoreDoesNotSupportProtection()
    {
        var services = new ServiceCollection()
                .AddLogging();
        services.AddIdentity<PocoUser, PocoRole>(o => o.Stores.ProtectPersonalData = true)
            .AddUserStore<NoopUserStore>();
        var e = Assert.Throws<InvalidOperationException>(() => services.BuildServiceProvider().GetService<UserManager<PocoUser>>());
        Assert.Contains("Store does not implement IProtectedUserStore", e.Message);
    }

    [Fact]
    public void UserManagerThrowsIfMissingPersonalDataProtection()
    {
        var services = new ServiceCollection()
                .AddLogging();
        services.AddIdentity<PocoUser, PocoRole>(o => o.Stores.ProtectPersonalData = true)
            .AddUserStore<ProtectedStore>();
        var e = Assert.Throws<InvalidOperationException>(() => services.BuildServiceProvider().GetService<UserManager<PocoUser>>());
        Assert.Contains("No IPersonalDataProtector service was registered", e.Message);
    }

    private class ProtectedStore : IProtectedUserStore<PocoUser>
    {
        public Task<IdentityResult> CreateAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> DeleteAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<PocoUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PocoUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedUserNameAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserIdAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedUserNameAsync(PocoUser user, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void UserManagerWillUseTokenProviderInstanceOverDefaults()
    {
        var provider = new ATokenProvider();
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddLogging();

        services.AddIdentity<PocoUser, PocoRole>(o => o.Tokens.ProviderMap.Add(TokenOptions.DefaultProvider, new TokenProviderDescriptor(typeof(ATokenProvider))
        {
            ProviderInstance = provider
        })).AddUserStore<NoopUserStore>().AddDefaultTokenProviders();
        var manager = services.BuildServiceProvider().GetService<UserManager<PocoUser>>();
        Assert.ThrowsAsync<NotImplementedException>(() => manager.GenerateUserTokenAsync(new PocoUser(), TokenOptions.DefaultProvider, "purpose"));
    }

    [Fact]
    public async Task TwoFactorStoreMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserTwoFactor);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetTwoFactorEnabledAsync(null));
        await
            Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetTwoFactorEnabledAsync(null, true));
    }

    [Fact]
    public async Task LockoutStoreMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserLockout);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLockoutEnabledAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetLockoutEnabledAsync(null, true));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AccessFailedAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.IsLockedOutAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.ResetAccessFailedCountAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetAccessFailedCountAsync(null));
    }

    [Fact]
    public async Task RoleMethodsFailWhenStoreNotImplemented()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserRole);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddToRoleAsync(null, "bogus"));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddToRolesAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetRolesAsync(null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveFromRoleAsync(null, "bogus"));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveFromRolesAsync(null, null));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.IsInRoleAsync(null, "bogus"));
    }

    [Fact]
    public async Task AuthTokenMethodsFailWhenStoreNotImplemented()
    {
        var error = Extensions.Identity.Core.Resources.StoreNotIUserAuthenticationTokenStore;
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserAuthenticationTokens);
        await VerifyException<NotSupportedException>(async () => await manager.GetAuthenticationTokenAsync(null, null, null), error);
        await VerifyException<NotSupportedException>(async () => await manager.SetAuthenticationTokenAsync(null, null, null, null), error);
        await VerifyException<NotSupportedException>(async () => await manager.RemoveAuthenticationTokenAsync(null, null, null), error);
    }

    [Fact]
    public async Task AuthenticatorMethodsFailWhenStoreNotImplemented()
    {
        var error = Extensions.Identity.Core.Resources.StoreNotIUserAuthenticatorKeyStore;
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserAuthenticatorKey);
        await VerifyException<NotSupportedException>(async () => await manager.GetAuthenticatorKeyAsync(null), error);
        await VerifyException<NotSupportedException>(async () => await manager.ResetAuthenticatorKeyAsync(null), error);
    }

    [Fact]
    public async Task RecoveryMethodsFailWhenStoreNotImplemented()
    {
        var error = Extensions.Identity.Core.Resources.StoreNotIUserTwoFactorRecoveryCodeStore;
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        Assert.False(manager.SupportsUserTwoFactorRecoveryCodes);
        await VerifyException<NotSupportedException>(async () => await manager.RedeemTwoFactorRecoveryCodeAsync(null, null), error);
        await VerifyException<NotSupportedException>(async () => await manager.GenerateNewTwoFactorRecoveryCodesAsync(null, 10), error);
    }

    private async Task VerifyException<TException>(Func<Task> code, string expectedMessage) where TException : Exception
    {
        var error = await Assert.ThrowsAsync<TException>(code);
        Assert.Equal(expectedMessage, error.Message);
    }

    [Fact]
    public void DisposeAfterDisposeDoesNotThrow()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public async Task PasswordValidatorBlocksCreate()
    {
        var manager = MockHelpers.TestUserManager(new EmptyStore());
        manager.PasswordValidators.Clear();
        manager.PasswordValidators.Add(new BadPasswordValidator<PocoUser>(true));
        IdentityResultAssert.IsFailure(await manager.CreateAsync(new PocoUser(), "password"),
            BadPasswordValidator<PocoUser>.ErrorMessage);
    }

    [Fact]
    public async Task PasswordValidatorWithoutErrorsBlocksCreate()
    {
        var manager = MockHelpers.TestUserManager(new EmptyStore());
        manager.PasswordValidators.Clear();
        manager.PasswordValidators.Add(new BadPasswordValidator<PocoUser>());
        IdentityResultAssert.IsFailure(await manager.CreateAsync(new PocoUser(), "password"));
    }

    [Fact]
    public async Task ResetTokenCallNoopForTokenValueZero()
    {
        var user = new PocoUser() { UserName = Guid.NewGuid().ToString() };
        var store = new Mock<IUserLockoutStore<PocoUser>>();
        store.Setup(x => x.ResetAccessFailedCountAsync(user, It.IsAny<CancellationToken>())).Returns(() =>
           {
               throw new Exception();
           });
        var manager = MockHelpers.TestUserManager(store.Object);

        IdentityResultAssert.IsSuccess(await manager.ResetAccessFailedCountAsync(user));
    }

    [Fact]
    public async Task ManagerPublicNullChecks()
    {
        Assert.Throws<ArgumentNullException>("store",
            () => new UserManager<PocoUser>(null, null, null, null, null, null, null, null, null));

        var manager = MockHelpers.TestUserManager(new NotImplementedStore());

        await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null, null));
        await
            Assert.ThrowsAsync<ArgumentNullException>("password",
                async () => await manager.CreateAsync(new PocoUser(), null));
        await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.UpdateAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.DeleteAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("claim", async () => await manager.AddClaimAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("claim", async () => await manager.ReplaceClaimAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("claims", async () => await manager.AddClaimsAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.FindByNameAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.AddLoginAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("loginProvider",
            async () => await manager.RemoveLoginAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("providerKey",
            async () => await manager.RemoveLoginAsync(null, "", null));
        await Assert.ThrowsAsync<ArgumentNullException>("email", async () => await manager.FindByEmailAsync(null));
        Assert.Throws<ArgumentNullException>("provider", () => manager.RegisterTokenProvider("whatever", null));
        await Assert.ThrowsAsync<ArgumentNullException>("roles", async () => await manager.AddToRolesAsync(new PocoUser(), null));
        await Assert.ThrowsAsync<ArgumentNullException>("roles", async () => await manager.RemoveFromRolesAsync(new PocoUser(), null));
    }

    [Fact]
    public async Task MethodsFailWithUnknownUserTest()
    {
        var manager = MockHelpers.TestUserManager(new EmptyStore());
        manager.RegisterTokenProvider("whatever", new NoOpTokenProvider());
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetUserNameAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.SetUserNameAsync(null, "bogus"));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.AddClaimAsync(null, new Claim("a", "b")));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.AddLoginAsync(null, new UserLoginInfo("", "", "")));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.AddPasswordAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.AddToRoleAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.AddToRolesAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.ChangePasswordAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetClaimsAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetLoginsAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetRolesAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.IsInRoleAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.RemoveClaimAsync(null, new Claim("a", "b")));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.RemoveLoginAsync(null, "", ""));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.RemovePasswordAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.RemoveFromRoleAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.RemoveFromRolesAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.ReplaceClaimAsync(null, new Claim("a", "b"), new Claim("a", "c")));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.UpdateSecurityStampAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetSecurityStampAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.HasPasswordAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GeneratePasswordResetTokenAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.ResetPasswordAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.IsEmailConfirmedAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GenerateEmailConfirmationTokenAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.ConfirmEmailAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetEmailAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.SetEmailAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.IsPhoneNumberConfirmedAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.ChangePhoneNumberAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.VerifyChangePhoneNumberTokenAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetPhoneNumberAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.SetPhoneNumberAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetTwoFactorEnabledAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.SetTwoFactorEnabledAsync(null, true));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GenerateTwoFactorTokenAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.VerifyTwoFactorTokenAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetValidTwoFactorProvidersAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.VerifyUserTokenAsync(null, null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.AccessFailedAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.ResetAccessFailedCountAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetAccessFailedCountAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetLockoutEnabledAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.SetLockoutEnabledAsync(null, false));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.SetLockoutEndDateAsync(null, DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.GetLockoutEndDateAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await manager.IsLockedOutAsync(null));
    }

    [Fact]
    public async Task MethodsThrowWhenDisposedTest()
    {
        var manager = MockHelpers.TestUserManager(new NoopUserStore());
        manager.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddClaimAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddClaimsAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddLoginAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddPasswordAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddToRoleAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddToRolesAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ChangePasswordAsync(null, null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetClaimsAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetLoginsAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetRolesAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.IsInRoleAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveClaimAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveClaimsAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveLoginAsync(null, null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemovePasswordAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveFromRoleAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveFromRolesAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByLoginAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByIdAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByNameAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.DeleteAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ReplaceClaimAsync(null, null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateSecurityStampAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetSecurityStampAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GeneratePasswordResetTokenAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ResetPasswordAsync(null, null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GenerateEmailConfirmationTokenAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.IsEmailConfirmedAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ConfirmEmailAsync(null, null));
    }

    private class BadPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : class
    {
        public static readonly IdentityError ErrorMessage = new IdentityError { Description = "I'm Bad." };

        private IdentityResult badResult;

        public BadPasswordValidator(bool includeErrorMessage = false)
        {
            if (includeErrorMessage)
            {
                badResult = IdentityResult.Failed(ErrorMessage);
            }
            else
            {
                badResult = IdentityResult.Failed();
            }
        }

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
            => Task.FromResult(badResult);
    }

    private class EmptyStore :
        IUserPasswordStore<PocoUser>,
        IUserClaimStore<PocoUser>,
        IUserLoginStore<PocoUser>,
        IUserEmailStore<PocoUser>,
        IUserPhoneNumberStore<PocoUser>,
        IUserLockoutStore<PocoUser>,
        IUserTwoFactorStore<PocoUser>,
        IUserRoleStore<PocoUser>,
        IUserSecurityStampStore<PocoUser>
    {
        public Task<IList<Claim>> GetClaimsAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IList<Claim>>(new List<Claim>());
        }

        public Task AddClaimsAsync(PocoUser user, IEnumerable<Claim> claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task ReplaceClaimAsync(PocoUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task RemoveClaimsAsync(PocoUser user, IEnumerable<Claim> claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task SetEmailAsync(PocoUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult("");
        }

        public Task<bool> GetEmailConfirmedAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        public Task SetEmailConfirmedAsync(PocoUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<PocoUser> FindByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<PocoUser>(null);
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<DateTimeOffset?>(DateTimeOffset.MinValue);
        }

        public Task SetLockoutEndDateAsync(PocoUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<int> IncrementAccessFailedCountAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task ResetAccessFailedCountAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCountAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<bool> GetLockoutEnabledAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        public Task SetLockoutEnabledAsync(PocoUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task AddLoginAsync(PocoUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task RemoveLoginAsync(PocoUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>());
        }

        public Task<PocoUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<PocoUser>(null);
        }

        public void Dispose()
        {
        }

        public Task SetUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<IdentityResult> CreateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<PocoUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<PocoUser>(null);
        }

        public Task<PocoUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<PocoUser>(null);
        }

        public Task SetPasswordHashAsync(PocoUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<string>(null);
        }

        public Task<bool> HasPasswordAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        public Task SetPhoneNumberAsync(PocoUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult("");
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        public Task SetPhoneNumberConfirmedAsync(PocoUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task AddToRoleAsync(PocoUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task RemoveFromRoleAsync(PocoUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<IList<string>> GetRolesAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        public Task<bool> IsInRoleAsync(PocoUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        public Task SetSecurityStampAsync(PocoUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult("");
        }

        public Task SetTwoFactorEnabledAsync(PocoUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        public Task<string> GetUserIdAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<string>(null);
        }

        public Task<string> GetUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<string>(null);
        }

        public Task<string> GetNormalizedUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<string>(null);
        }

        public Task SetNormalizedUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<IList<PocoUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IList<PocoUser>>(new List<PocoUser>());
        }

        public Task<IList<PocoUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IList<PocoUser>>(new List<PocoUser>());
        }

        public Task<string> GetNormalizedEmailAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult("");
        }

        public Task SetNormalizedEmailAsync(PocoUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
    }

    private class NoOpTokenProvider : IUserTwoFactorTokenProvider<PocoUser>
    {
        public string Name { get; } = "Noop";

        public Task<string> GenerateAsync(string purpose, UserManager<PocoUser> manager, PocoUser user)
        {
            return Task.FromResult("Test");
        }

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<PocoUser> manager, PocoUser user)
        {
            return Task.FromResult(true);
        }

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<PocoUser> manager, PocoUser user)
        {
            return Task.FromResult(true);
        }
    }

    private class NotImplementedStore :
        IUserPasswordStore<PocoUser>,
        IUserClaimStore<PocoUser>,
        IUserLoginStore<PocoUser>,
        IUserRoleStore<PocoUser>,
        IUserEmailStore<PocoUser>,
        IUserPhoneNumberStore<PocoUser>,
        IUserLockoutStore<PocoUser>,
        IUserTwoFactorStore<PocoUser>
    {
        public Task<IList<Claim>> GetClaimsAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task AddClaimsAsync(PocoUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task ReplaceClaimAsync(PocoUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task RemoveClaimsAsync(PocoUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetEmailAsync(PocoUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> GetEmailAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetEmailConfirmedAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetEmailConfirmedAsync(PocoUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PocoUser> FindByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetLockoutEndDateAsync(PocoUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<int> IncrementAccessFailedCountAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task ResetAccessFailedCountAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<int> GetAccessFailedCountAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetLockoutEnabledAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetLockoutEnabledAsync(PocoUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task AddLoginAsync(PocoUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task RemoveLoginAsync(PocoUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PocoUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserIdAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PocoUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PocoUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetPasswordHashAsync(PocoUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPasswordHashAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasPasswordAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetPhoneNumberAsync(PocoUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPhoneNumberAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetPhoneNumberConfirmedAsync(PocoUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetTwoFactorEnabledAsync(PocoUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetTwoFactorEnabledAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task AddToRoleAsync(PocoUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task RemoveFromRoleAsync(PocoUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IList<string>> GetRolesAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsInRoleAsync(PocoUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IList<PocoUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IList<PocoUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        Task<IdentityResult> IUserStore<PocoUser>.CreateAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IdentityResult> IUserStore<PocoUser>.UpdateAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IdentityResult> IUserStore<PocoUser>.DeleteAsync(PocoUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedEmailAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedEmailAsync(PocoUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task CanCustomizeUserValidatorErrors()
    {
        var store = new Mock<IUserEmailStore<PocoUser>>();
        var describer = new TestErrorDescriber();
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddLogging()
                .AddSingleton<IdentityErrorDescriber>(describer)
                .AddSingleton<IUserStore<PocoUser>>(store.Object)
                .AddHttpContextAccessor();

        services.AddIdentity<PocoUser, PocoRole>();

        var manager = services.BuildServiceProvider().GetRequiredService<UserManager<PocoUser>>();

        manager.Options.User.RequireUniqueEmail = true;
        var user = new PocoUser() { UserName = "dupeEmail", Email = "dupe@email.com" };
        var user2 = new PocoUser() { UserName = "dupeEmail2", Email = "dupe@email.com" };
        store.Setup(s => s.FindByEmailAsync("DUPE@EMAIL.COM", CancellationToken.None))
            .Returns(Task.FromResult(user2))
            .Verifiable();
        store.Setup(s => s.GetUserIdAsync(user2, CancellationToken.None))
            .Returns(Task.FromResult(user2.Id))
            .Verifiable();
        store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None))
            .Returns(Task.FromResult(user.UserName))
            .Verifiable();
        store.Setup(s => s.GetEmailAsync(user, CancellationToken.None))
            .Returns(Task.FromResult(user.Email))
            .Verifiable();

        Assert.Same(describer, manager.ErrorDescriber);
        IdentityResultAssert.IsFailure(await manager.CreateAsync(user), describer.DuplicateEmail(user.Email));

        store.VerifyAll();
    }

    public class TestErrorDescriber : IdentityErrorDescriber
    {
        public static string Code = "Error";
        public static string FormatError = "FormatError {0}";

        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError { Code = Code, Description = string.Format(CultureInfo.InvariantCulture, FormatError, email) };
        }
    }

}
