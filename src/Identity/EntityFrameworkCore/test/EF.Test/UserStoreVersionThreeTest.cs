// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

/// <summary>
/// Tests for Version 3 schema changes to UserTokens and UserLogins with Id primary keys
/// </summary>
public class UserStoreVersionThreeTest : IClassFixture<ScratchDatabaseFixture>
{
    private readonly ScratchDatabaseFixture _fixture;

    public UserStoreVersionThreeTest(ScratchDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private VersionThreeDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddDbContext<VersionThreeDbContext>(o =>
                o.UseSqlite(_fixture.Connection)
                    .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)))
            .AddIdentity<IdentityUser, IdentityRole>(o =>
            {
                o.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddEntityFrameworkStores<VersionThreeDbContext>();

        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VersionThreeDbContext>();
        db.Database.EnsureCreated();

        return db;
    }

    [Fact]
    public void VerifyV3SchemaHasIdPrimaryKeys()
    {
        using var db = CreateContext();
        using var sqlConn = (SqliteConnection)db.Database.GetDbConnection();
        sqlConn.Open();

        // Verify UserLogins has Id column
        Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserLogins", "Id", "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));

        // Verify UserTokens has Id column
        Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserTokens", "Id", "UserId", "LoginProvider", "Name", "Value"));
    }

    [Fact]
    public void VerifyV3SchemaHasUniqueIndexes()
    {
        using var db = CreateContext();
        using var sqlConn = (SqliteConnection)db.Database.GetDbConnection();
        sqlConn.Open();

        // Verify unique index on UserLogins (LoginProvider, ProviderKey)
        DbUtil.VerifyIndex(sqlConn, "AspNetUserLogins", "IX_AspNetUserLogins_LoginProvider_ProviderKey", isUnique: true);

        // Verify unique index on UserTokens (UserId, LoginProvider, Name)
        DbUtil.VerifyIndex(sqlConn, "AspNetUserTokens", "IX_AspNetUserTokens_UserId_LoginProvider_Name", isUnique: true);
    }

    [Fact]
    public async Task CanSetAndGetTokenWithV3Schema()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "TokenTestUser" };
        await store.CreateAsync(user, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";
        const string tokenValue = "TestValue";

        // Set token
        await store.SetTokenAsync(user, loginProvider, tokenName, tokenValue, CancellationToken.None);
        await db.SaveChangesAsync();

        // Get token
        var retrievedValue = await store.GetTokenAsync(user, loginProvider, tokenName, CancellationToken.None);
        Assert.Equal(tokenValue, retrievedValue);
    }

    [Fact]
    public async Task CanUpdateTokenWithV3Schema()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "UpdateTokenUser" };
        await store.CreateAsync(user, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";
        const string initialValue = "InitialValue";
        const string updatedValue = "UpdatedValue";

        // Set initial token
        await store.SetTokenAsync(user, loginProvider, tokenName, initialValue, CancellationToken.None);
        await db.SaveChangesAsync();

        // Update token
        await store.SetTokenAsync(user, loginProvider, tokenName, updatedValue, CancellationToken.None);
        await db.SaveChangesAsync();

        // Verify updated value
        var retrievedValue = await store.GetTokenAsync(user, loginProvider, tokenName, CancellationToken.None);
        Assert.Equal(updatedValue, retrievedValue);
    }

    [Fact]
    public async Task CanRemoveTokenWithV3Schema()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "RemoveTokenUser" };
        await store.CreateAsync(user, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";
        const string tokenValue = "TestValue";

        // Set token
        await store.SetTokenAsync(user, loginProvider, tokenName, tokenValue, CancellationToken.None);
        await db.SaveChangesAsync();

        // Remove token
        await store.RemoveTokenAsync(user, loginProvider, tokenName, CancellationToken.None);
        await db.SaveChangesAsync();

        // Verify token is removed
        var retrievedValue = await store.GetTokenAsync(user, loginProvider, tokenName, CancellationToken.None);
        Assert.Null(retrievedValue);
    }

    [Fact]
    public async Task CanHandleMultipleTokensForSameUser()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "MultiTokenUser" };
        await store.CreateAsync(user, CancellationToken.None);

        // Set multiple tokens
        await store.SetTokenAsync(user, "Provider1", "Token1", "Value1", CancellationToken.None);
        await store.SetTokenAsync(user, "Provider1", "Token2", "Value2", CancellationToken.None);
        await store.SetTokenAsync(user, "Provider2", "Token1", "Value3", CancellationToken.None);
        await db.SaveChangesAsync();

        // Verify all tokens
        Assert.Equal("Value1", await store.GetTokenAsync(user, "Provider1", "Token1", CancellationToken.None));
        Assert.Equal("Value2", await store.GetTokenAsync(user, "Provider1", "Token2", CancellationToken.None));
        Assert.Equal("Value3", await store.GetTokenAsync(user, "Provider2", "Token1", CancellationToken.None));
    }

    [Fact]
    public async Task TokensAreIsolatedBetweenUsers()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user1 = new IdentityUser { UserName = "TokenIsolationUser1" };
        var user2 = new IdentityUser { UserName = "TokenIsolationUser2" };
        await store.CreateAsync(user1, CancellationToken.None);
        await store.CreateAsync(user2, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";

        // Set different tokens for each user
        await store.SetTokenAsync(user1, loginProvider, tokenName, "User1Value", CancellationToken.None);
        await store.SetTokenAsync(user2, loginProvider, tokenName, "User2Value", CancellationToken.None);
        await db.SaveChangesAsync();

        // Verify tokens are isolated
        Assert.Equal("User1Value", await store.GetTokenAsync(user1, loginProvider, tokenName, CancellationToken.None));
        Assert.Equal("User2Value", await store.GetTokenAsync(user2, loginProvider, tokenName, CancellationToken.None));
    }

    [Fact]
    public async Task CanAddAndFindLoginWithV3Schema()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "LoginTestUser" };
        await store.CreateAsync(user, CancellationToken.None);

        var login = new UserLoginInfo("TestProvider", "AddFindLoginKey_" + Guid.NewGuid(), "TestDisplayName");

        // Add login
        await store.AddLoginAsync(user, login, CancellationToken.None);
        await db.SaveChangesAsync();

        // Find by login
        var foundUser = await store.FindByLoginAsync(login.LoginProvider, login.ProviderKey, CancellationToken.None);
        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);
    }

    [Fact]
    public async Task CanGetLoginsForUserWithV3Schema()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "GetLoginsUser" };
        await store.CreateAsync(user, CancellationToken.None);

        var login1 = new UserLoginInfo("Provider1", "Key1", "Display1");
        var login2 = new UserLoginInfo("Provider2", "Key2", "Display2");

        // Add logins
        await store.AddLoginAsync(user, login1, CancellationToken.None);
        await store.AddLoginAsync(user, login2, CancellationToken.None);
        await db.SaveChangesAsync();

        // Get logins
        var logins = await store.GetLoginsAsync(user, CancellationToken.None);
        Assert.Equal(2, logins.Count);
        Assert.Contains(logins, l => l.LoginProvider == "Provider1" && l.ProviderKey == "Key1");
        Assert.Contains(logins, l => l.LoginProvider == "Provider2" && l.ProviderKey == "Key2");
    }

    [Fact]
    public async Task CanRemoveLoginWithV3Schema()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "RemoveLoginUser" };
        await store.CreateAsync(user, CancellationToken.None);

        var login = new UserLoginInfo("TestProvider", "RemoveLoginKey_" + Guid.NewGuid(), "TestDisplayName");

        // Add login
        await store.AddLoginAsync(user, login, CancellationToken.None);
        await db.SaveChangesAsync();

        // Remove login
        await store.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey, CancellationToken.None);
        await db.SaveChangesAsync();

        // Verify login is removed
        var foundUser = await store.FindByLoginAsync(login.LoginProvider, login.ProviderKey, CancellationToken.None);
        Assert.Null(foundUser);

        var logins = await store.GetLoginsAsync(user, CancellationToken.None);
        Assert.Empty(logins);
    }

    [Fact]
    public async Task UniqueIndexPreventsIdDuplicateLogins()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "DuplicateLoginUser" };
        await store.CreateAsync(user, CancellationToken.None);

        var login = new UserLoginInfo("TestProvider", "UniqueLoginKey_" + Guid.NewGuid(), "TestDisplayName");

        // Add login first time
        await store.AddLoginAsync(user, login, CancellationToken.None);
        await db.SaveChangesAsync();

        // Attempt to add same login again should fail due to unique index
        await store.AddLoginAsync(user, login, CancellationToken.None);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueIndexPreventsDuplicateTokens()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "DuplicateTokenUser" };
        await store.CreateAsync(user, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";

        // Set token first time
        await store.SetTokenAsync(user, loginProvider, tokenName, "Value1", CancellationToken.None);
        await db.SaveChangesAsync();

        // Setting same token again should update, not create duplicate
        await store.SetTokenAsync(user, loginProvider, tokenName, "Value2", CancellationToken.None);
        await db.SaveChangesAsync();

        // Verify only one token exists with updated value
        var value = await store.GetTokenAsync(user, loginProvider, tokenName, CancellationToken.None);
        Assert.Equal("Value2", value);

        // Count tokens directly in database
        var tokenCount = await db.UserTokens
            .Where(t => t.UserId == user.Id && t.LoginProvider == loginProvider && t.Name == tokenName)
            .CountAsync();
        Assert.Equal(1, tokenCount);
    }

    [Fact]
    public async Task FindTokenByUniqueIndexAsyncWorksWithV3Schema()
    {
        using var db = CreateContext();
        var store = new UserOnlyStore<IdentityUser, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "FindTokenByIndexUser" };
        await store.CreateAsync(user, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";
        const string tokenValue = "TestValue";

        // Set token
        await store.SetTokenAsync(user, loginProvider, tokenName, tokenValue, CancellationToken.None);
        await db.SaveChangesAsync();

        // Verify token can be retrieved (internally uses FindTokenByUniqueIndexAsync for V3)
        var retrievedValue = await store.GetTokenAsync(user, loginProvider, tokenName, CancellationToken.None);
        Assert.Equal(tokenValue, retrievedValue);
    }

    [Fact]
    public async Task V3SchemaSupportsNullTokenValues()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "NullTokenUser" };
        await store.CreateAsync(user, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";

        // Set token with null value
        await store.SetTokenAsync(user, loginProvider, tokenName, null, CancellationToken.None);
        await db.SaveChangesAsync();

        // Retrieve null token
        var retrievedValue = await store.GetTokenAsync(user, loginProvider, tokenName, CancellationToken.None);
        Assert.Null(retrievedValue);
    }

    [Fact]
    public async Task V3SchemaTokenOperationsAreTransactional()
    {
        using var db = CreateContext();
        var store = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(db);

        var user = new IdentityUser { UserName = "TransactionalUser" };
        await store.CreateAsync(user, CancellationToken.None);

        const string loginProvider = "TestProvider";
        const string tokenName = "TestToken";

        // Set token but don't save
        await store.SetTokenAsync(user, loginProvider, tokenName, "Value1", CancellationToken.None);

        // Verify token is not persisted yet
        using (var newDb = CreateContext())
        {
            var newStore = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(newDb);
            var foundUser = await newStore.FindByIdAsync(user.Id, CancellationToken.None);
            var value = await newStore.GetTokenAsync(foundUser!, loginProvider, tokenName, CancellationToken.None);
            Assert.Null(value);
        }

        // Save changes
        await db.SaveChangesAsync();

        // Verify token is now persisted
        using (var newDb = CreateContext())
        {
            var newStore = new UserStore<IdentityUser, IdentityRole, VersionThreeDbContext>(newDb);
            var foundUser = await newStore.FindByIdAsync(user.Id, CancellationToken.None);
            var value = await newStore.GetTokenAsync(foundUser!, loginProvider, tokenName, CancellationToken.None);
            Assert.Equal("Value1", value);
        }
    }
}
