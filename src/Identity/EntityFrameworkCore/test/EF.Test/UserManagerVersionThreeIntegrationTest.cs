// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

/// <summary>
/// UserManager integration tests for V3 schema with Id primary keys on UserTokens and UserLogins
/// </summary>
public class UserManagerVersionThreeIntegrationTest : UserManagerSpecificationTestBase<IdentityUser, string>, IClassFixture<ScratchDatabaseFixture>
{
    private readonly ScratchDatabaseFixture _fixture;

    public UserManagerVersionThreeIntegrationTest(ScratchDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    protected override object CreateTestContext()
    {
        var db = DbUtil.Create<VersionThreeDbContext>(_fixture.Connection);
        db.Database.EnsureCreated();
        return db;
    }

    protected override void AddUserStore(IServiceCollection services, object context = null)
    {
        services.AddSingleton<IUserStore<IdentityUser>>(new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>((VersionThreeDbContext)context));
    }

    protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
    {
        user.PasswordHash = hashedPassword;
    }

    protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
        bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = null, bool useNamePrefixAsUserName = false)
    {
        return new IdentityUser
        {
            UserName = useNamePrefixAsUserName ? namePrefix : string.Format(CultureInfo.InvariantCulture, "{0}{1}", namePrefix, Guid.NewGuid()),
            Email = email,
            PhoneNumber = phoneNumber,
            LockoutEnabled = lockoutEnabled,
            LockoutEnd = lockoutEnd
        };
    }

    protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

    protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

    [Fact]
    public async Task UserManager_CanSetGetAndRemoveAuthenticationToken_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";
        const string tokenValue = "TestValue";

        // Set token
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, loginProvider, tokenName, tokenValue));

        // Get token
        var retrievedValue = await manager.GetAuthenticationTokenAsync(user, loginProvider, tokenName);
        Assert.Equal(tokenValue, retrievedValue);

        // Remove token
        IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user, loginProvider, tokenName));

        // Verify token is removed
        var removedValue = await manager.GetAuthenticationTokenAsync(user, loginProvider, tokenName);
        Assert.Null(removedValue);
    }

    [Fact]
    public async Task UserManager_CanUpdateAuthenticationToken_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";

        // Set initial token
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, loginProvider, tokenName, "InitialValue"));
        Assert.Equal("InitialValue", await manager.GetAuthenticationTokenAsync(user, loginProvider, tokenName));

        // Update token
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, loginProvider, tokenName, "UpdatedValue"));
        Assert.Equal("UpdatedValue", await manager.GetAuthenticationTokenAsync(user, loginProvider, tokenName));
    }

    [Fact]
    public async Task UserManager_CanAddFindAndRemoveLogin_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var login = new UserLoginInfo("TestProvider", "UserManager_CanAddFind_" + Guid.NewGuid(), "TestDisplayName");

        // Add login
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));

        // Find by login
        var foundUser = await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey);
        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);

        // Get logins
        var logins = await manager.GetLoginsAsync(user);
        Assert.Single(logins);
        Assert.Equal(login.LoginProvider, logins[0].LoginProvider);
        Assert.Equal(login.ProviderKey, logins[0].ProviderKey);

        // Remove login
        IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey));

        // Verify removal
        Assert.Null(await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
        logins = await manager.GetLoginsAsync(user);
        Assert.Empty(logins);
    }

    [Fact]
    public async Task UserManager_CannotAddDuplicateLogin_WithV3Schema()
    {
        var manager = CreateManager();
        var user1 = CreateTestUser("user1");
        var user2 = CreateTestUser("user2");

        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user1));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));

        var login = new UserLoginInfo("TestProvider", "TestProviderKey", "TestDisplayName");

        // Add login to first user
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user1, login));

        // Attempt to add same login to second user should fail
        var result = await manager.AddLoginAsync(user2, login);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "LoginAlreadyAssociated");
    }

    [Fact]
    public async Task UserManager_MultipleTokensPerUser_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        // Set multiple tokens
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "Provider1", "Token1", "Value1"));
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "Provider1", "Token2", "Value2"));
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "Provider2", "Token1", "Value3"));

        // Verify all tokens
        Assert.Equal("Value1", await manager.GetAuthenticationTokenAsync(user, "Provider1", "Token1"));
        Assert.Equal("Value2", await manager.GetAuthenticationTokenAsync(user, "Provider1", "Token2"));
        Assert.Equal("Value3", await manager.GetAuthenticationTokenAsync(user, "Provider2", "Token1"));

        // Remove one token
        IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user, "Provider1", "Token1"));

        // Verify only the removed token is gone
        Assert.Null(await manager.GetAuthenticationTokenAsync(user, "Provider1", "Token1"));
        Assert.Equal("Value2", await manager.GetAuthenticationTokenAsync(user, "Provider1", "Token2"));
        Assert.Equal("Value3", await manager.GetAuthenticationTokenAsync(user, "Provider2", "Token1"));
    }

    [Fact]
    public async Task UserManager_MultipleLoginsPerUser_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var login1 = new UserLoginInfo("Provider1", "Key1", "Display1");
        var login2 = new UserLoginInfo("Provider2", "Key2", "Display2");
        var login3 = new UserLoginInfo("Provider3", "Key3", "Display3");

        // Add multiple logins
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login1));
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login2));
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login3));

        // Get logins
        var logins = await manager.GetLoginsAsync(user);
        Assert.Equal(3, logins.Count);

        // Verify each login
        Assert.Contains(logins, l => l.LoginProvider == "Provider1" && l.ProviderKey == "Key1");
        Assert.Contains(logins, l => l.LoginProvider == "Provider2" && l.ProviderKey == "Key2");
        Assert.Contains(logins, l => l.LoginProvider == "Provider3" && l.ProviderKey == "Key3");

        // Remove one login
        IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login2.LoginProvider, login2.ProviderKey));

        // Verify remaining logins
        logins = await manager.GetLoginsAsync(user);
        Assert.Equal(2, logins.Count);
        Assert.Contains(logins, l => l.LoginProvider == "Provider1");
        Assert.Contains(logins, l => l.LoginProvider == "Provider3");
        Assert.DoesNotContain(logins, l => l.LoginProvider == "Provider2");
    }

    [Fact]
    public async Task UserManager_TokensIsolatedBetweenUsers_WithV3Schema()
    {
        var manager = CreateManager();
        var user1 = CreateTestUser("isolation1");
        var user2 = CreateTestUser("isolation2");

        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user1));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";

        // Set different tokens for each user
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user1, loginProvider, tokenName, "User1Value"));
        IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user2, loginProvider, tokenName, "User2Value"));

        // Verify isolation
        Assert.Equal("User1Value", await manager.GetAuthenticationTokenAsync(user1, loginProvider, tokenName));
        Assert.Equal("User2Value", await manager.GetAuthenticationTokenAsync(user2, loginProvider, tokenName));

        // Remove token from user1
        IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user1, loginProvider, tokenName));

        // Verify user2's token is unaffected
        Assert.Null(await manager.GetAuthenticationTokenAsync(user1, loginProvider, tokenName));
        Assert.Equal("User2Value", await manager.GetAuthenticationTokenAsync(user2, loginProvider, tokenName));
    }

    [Fact]
    public async Task UserManager_LoginsIsolatedBetweenUsers_WithV3Schema()
    {
        var manager = CreateManager();
        var user1 = CreateTestUser("loginiso1");
        var user2 = CreateTestUser("loginiso2");

        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user1));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));

        var login1 = new UserLoginInfo("Provider", "Key1", "Display");
        var login2 = new UserLoginInfo("Provider", "Key2", "Display");

        // Add different logins to each user
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user1, login1));
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user2, login2));

        // Verify each user can be found by their login
        var foundUser1 = await manager.FindByLoginAsync(login1.LoginProvider, login1.ProviderKey);
        var foundUser2 = await manager.FindByLoginAsync(login2.LoginProvider, login2.ProviderKey);

        Assert.Equal(user1.Id, foundUser1.Id);
        Assert.Equal(user2.Id, foundUser2.Id);
    }

    [Fact]
    public async Task UserManager_SecurityStampChangesOnLoginRemoval_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var login = new UserLoginInfo("TestProvider", "SecurityStampTest_" + Guid.NewGuid(), "TestDisplayName");

        // Add login and capture security stamp
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
        var stampAfterAdd = await manager.GetSecurityStampAsync(user);

        // Remove login and verify security stamp changed
        IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey));
        var stampAfterRemove = await manager.GetSecurityStampAsync(user);

        Assert.NotEqual(stampAfterAdd, stampAfterRemove);
    }

    [Fact]
    public async Task UserManager_CanRemoveNonExistentToken_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        // Removing non-existent token should succeed
        IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user, "NonExistentProvider", "NonExistentToken"));
    }

    [Fact]
    public async Task UserManager_CanRemoveNonExistentLogin_WithV3Schema()
    {
        var manager = CreateManager();
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        // Removing non-existent login should succeed
        IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, "NonExistentProvider", "NonExistentKey"));
    }
}
