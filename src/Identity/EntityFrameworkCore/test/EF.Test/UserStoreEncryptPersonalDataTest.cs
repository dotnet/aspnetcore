// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class ProtectedUserStoreTest : SqlStoreTestBase<IdentityUser, IdentityRole, string>
{
    public ProtectedUserStoreTest(ScratchDatabaseFixture fixture)
        : base(fixture)
    { }

    protected override void SetupAddIdentity(IServiceCollection services)
    {
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Stores.ProtectPersonalData = true;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.AllowedUserNameCharacters = null;
        })
        .AddDefaultTokenProviders()
        .AddEntityFrameworkStores<TestDbContext>()
        .AddPersonalDataProtection<SillyEncryptor, DefaultKeyRing>();
    }

    public class DefaultKeyRing : ILookupProtectorKeyRing
    {
        public static string Current = "Default";
        public string this[string keyId] => keyId;
        public string CurrentKeyId => Current;

        public IEnumerable<string> GetAllKeyIds()
        {
            return new string[] { "Default", "NewPad" };
        }
    }

    private class SillyEncryptor : ILookupProtector
    {
        private readonly ILookupProtectorKeyRing _keyRing;

        public SillyEncryptor(ILookupProtectorKeyRing keyRing) => _keyRing = keyRing;

        public string Unprotect(string keyId, string data)
        {
            var pad = _keyRing[keyId];
            if (!data.StartsWith(pad, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Didn't find pad.");
            }
            return data.Substring(pad.Length);
        }

        public string Protect(string keyId, string data)
            => _keyRing[keyId] + data;
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanRotateKeysAndStillFind()
    {
        var manager = CreateManager();
        var name = Guid.NewGuid().ToString();
        var user = CreateTestUser(name);
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, "hao@hao.com"));
        var newName = Guid.NewGuid().ToString();
        Assert.Null(await manager.FindByNameAsync(newName));
        IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "123-456-7890"));
        var login = new UserLoginInfo("loginProvider", "<key>", "display");
        IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));

        Assert.Equal(user, await manager.FindByEmailAsync("hao@hao.com"));
        Assert.Equal(user, await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));

        IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newName));
        IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
        Assert.NotNull(await manager.FindByNameAsync(newName));
        Assert.Null(await manager.FindByNameAsync(name));
        DefaultKeyRing.Current = "NewPad";
        Assert.NotNull(await manager.FindByNameAsync(newName));
        Assert.Equal(user, await manager.FindByEmailAsync("hao@hao.com"));
        Assert.Equal(user, await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
        Assert.Equal("123-456-7890", await manager.GetPhoneNumberAsync(user));
    }

    private class InkProtector : ILookupProtector
    {
        public InkProtector() { }

        public string Unprotect(string keyId, string data)
            => data?.Substring(4);

        public string Protect(string keyId, string data)
            => "ink:" + data;
    }

    private class CustomUser : IdentityUser
    {
        [ProtectedPersonalData]
        public string PersonalData1 { get; set; }
        public string NonPersonalData1 { get; set; }
        [ProtectedPersonalData]
        public string PersonalData2 { get; set; }
        public string NonPersonalData2 { get; set; }
        [PersonalData]
        public string SafePersonalData { get; set; }
    }

    private bool FindInk(DbConnection conn, string column, string id)
    {
        using (var command = conn.CreateCommand())
        {
            command.CommandText = $"SELECT u.{column} FROM AspNetUsers u WHERE u.Id = '{id}'";
            command.CommandType = System.Data.CommandType.Text;
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    var value = reader.GetString(0);
                    return value.StartsWith("Default:ink:", StringComparison.Ordinal);
                }
            }
        }
        Assert.False(true, "Didn't find user");
        return false;
    }

    private bool FindAuthenticatorKeyInk(DbConnection conn, string id)
        => FindTokenInk(conn, id, "[AspNetUserStore]", "AuthenticatorKey");

    private bool FindTokenInk(DbConnection conn, string id, string loginProvider, string tokenName)
    {
        using (var command = conn.CreateCommand())
        {
            command.CommandText = $"SELECT u.Value FROM AspNetUserTokens u WHERE u.LoginProvider = '{loginProvider}' AND u.Name = '{tokenName}' AND u.UserId = '{id}'";
            command.CommandType = System.Data.CommandType.Text;
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    var value = reader.GetString(0);
                    return value.StartsWith("Default:ink:", StringComparison.Ordinal);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public Task CustomPersonalDataPropertiesCanBeProtected()
        => CustomPersonalDataPropertiesAreProtected<ProtectedIdentityDbContext>(true);

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public Task CustomPersonalDataPropertiesCanBeNotProtected()
        => CustomPersonalDataPropertiesAreProtected<UnprotectedIdentityDbContext>(false);

    private async Task CustomPersonalDataPropertiesAreProtected<TContext>(bool protect)
        where TContext : DbContext
    {
        using (var connection = new SqliteConnection($"DataSource=D{Guid.NewGuid()}.db"))
        {
            var services = new ServiceCollection().AddLogging();
            services.AddIdentity<CustomUser, IdentityRole>(options =>
            {
                options.Stores.ProtectPersonalData = protect;
            })
                .AddEntityFrameworkStores<TContext>()
                .AddPersonalDataProtection<InkProtector, DefaultKeyRing>();

            services.AddDbContext<TContext>(b => b.UseSqlite(connection));

            var applicationServiceProvider = services.BuildServiceProvider();

            using (var scope = applicationServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                dbContext.Database.EnsureCreated();

                var manager = scope.ServiceProvider.GetService<UserManager<CustomUser>>();

                var guid = Guid.NewGuid().ToString();
                var user = new CustomUser();
                user.Id = guid;
                user.UserName = guid;
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
                user.Email = "test@test.com";
                user.PersonalData1 = "p1";
                user.PersonalData2 = "p2";
                user.NonPersonalData1 = "np1";
                user.NonPersonalData2 = "np2";
                user.SafePersonalData = "safe";
                user.PhoneNumber = "12345678";
                IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));

                IdentityResultAssert.IsSuccess(await manager.ResetAuthenticatorKeyAsync(user));
                IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "loginProvider", "token", "value"));

                connection.Open();
                if (protect)
                {
                    Assert.True(FindInk(connection, "PhoneNumber", guid));
                    Assert.True(FindInk(connection, "Email", guid));
                    Assert.True(FindInk(connection, "UserName", guid));
                    Assert.True(FindInk(connection, "PersonalData1", guid));
                    Assert.True(FindInk(connection, "PersonalData2", guid));
                    Assert.True(FindAuthenticatorKeyInk(connection, guid));
                    Assert.True(FindTokenInk(connection, guid, "loginProvider", "token"));
                }
                else
                {
                    Assert.False(FindInk(connection, "PhoneNumber", guid));
                    Assert.False(FindInk(connection, "Email", guid));
                    Assert.False(FindInk(connection, "UserName", guid));
                    Assert.False(FindInk(connection, "PersonalData1", guid));
                    Assert.False(FindInk(connection, "PersonalData2", guid));
                    Assert.False(FindAuthenticatorKeyInk(connection, guid));
                    Assert.False(FindTokenInk(connection, guid, "loginProvider", "token"));
                }

                Assert.False(FindInk(connection, "NonPersonalData1", guid));
                Assert.False(FindInk(connection, "NonPersonalData2", guid));
                Assert.False(FindInk(connection, "SafePersonalData", guid));

                connection.Close();
            }
        }
    }

    private class ProtectedIdentityDbContext : IdentityDbContext<CustomUser>
    {
        public ProtectedIdentityDbContext(DbContextOptions<ProtectedIdentityDbContext> options)
            : base(options)
        {
        }
    }

    private class UnprotectedIdentityDbContext : IdentityDbContext<CustomUser>
    {
        public UnprotectedIdentityDbContext(DbContextOptions<UnprotectedIdentityDbContext> options)
            : base(options)
        {
        }
    }

    private class InvalidUser : IdentityUser
    {
        [ProtectedPersonalData]
        public bool PersonalData1 { get; set; }
    }

    /// <summary>
    /// Test.
    /// </summary>
    [Fact]
    public void ProtectedPersonalDataThrowsOnNonString()
    {
        using (var scratch = new ScratchDatabaseFixture())
        {
            var services = new ServiceCollection().AddLogging();
            services.AddIdentity<CustomUser, IdentityRole>(options =>
            {
                options.Stores.ProtectPersonalData = true;
            })
                .AddEntityFrameworkStores<IdentityDbContext<CustomUser>>()
                .AddPersonalDataProtection<InkProtector, DefaultKeyRing>();
            var dbOptions = new DbContextOptionsBuilder().UseSqlite(scratch.Connection)
                .UseApplicationServiceProvider(services.BuildServiceProvider())
                .Options;
            var dbContext = new IdentityDbContext<InvalidUser>(dbOptions);
            var e = Assert.Throws<InvalidOperationException>(() => dbContext.Database.EnsureCreated());
            Assert.Equal("[ProtectedPersonalData] only works strings by default.", e.Message);
        }
    }

    /// <summary>
    /// Skipped because encryption causes this to fail.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public override Task CanFindUsersViaUserQueryable()
        => Task.CompletedTask;

}
