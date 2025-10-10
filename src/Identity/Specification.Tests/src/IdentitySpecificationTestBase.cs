// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test;

/// <summary>
/// Common functionality tests that all verifies user manager functionality regardless of store implementation
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
/// <typeparam name="TRole">The type of the role.</typeparam>
public abstract class IdentitySpecificationTestBase<TUser, TRole> : IdentitySpecificationTestBase<TUser, TRole, string>
    where TUser : class
    where TRole : class
{ }

/// <summary>
/// Base class for tests that exercise basic identity functionality that all stores should support.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
/// <typeparam name="TRole">The type of the role.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class IdentitySpecificationTestBase<TUser, TRole, TKey> : UserManagerSpecificationTestBase<TUser, TKey>
    where TUser : class
    where TRole : class
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Configure the service collection used for tests.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="context"></param>
    protected override void SetupIdentityServices(IServiceCollection services, object context)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
        services.AddIdentity<TUser, TRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.AllowedUserNameCharacters = null;
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        }).AddDefaultTokenProviders();
        AddUserStore(services, context);
        AddRoleStore(services, context);
        services.AddLogging();
        services.AddSingleton<ILogger<UserManager<TUser>>>(new TestLogger<UserManager<TUser>>());
        services.AddSingleton<ILogger<RoleManager<TRole>>>(new TestLogger<RoleManager<TRole>>());
    }

    /// <summary>
    /// Setup the IdentityBuilder
    /// </summary>
    /// <param name="services"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    protected override IdentityBuilder SetupBuilder(IServiceCollection services, object context)
    {
        var builder = base.SetupBuilder(services, context);
        builder.AddRoles<TRole>();
        AddRoleStore(services, context);
        services.AddSingleton<ILogger<RoleManager<TRole>>>(new TestLogger<RoleManager<TRole>>());
        return builder;
    }

    /// <summary>
    /// Creates the role manager for tests.
    /// </summary>
    /// <param name="context">The context that will be passed into the store, typically a db context.</param>
    /// <param name="services">The service collection to use, optional.</param>
    /// <returns></returns>
    protected virtual RoleManager<TRole> CreateRoleManager(object context = null, IServiceCollection services = null)
    {
        if (services == null)
        {
            services = new ServiceCollection();
        }
        if (context == null)
        {
            context = CreateTestContext();
        }
        SetupIdentityServices(services, context);
        return services.BuildServiceProvider().GetService<RoleManager<TRole>>();
    }

    /// <summary>
    /// Adds an IRoleStore to services for the test.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="context">The context for the store to use, optional.</param>
    protected abstract void AddRoleStore(IServiceCollection services, object context = null);

    /// <summary>
    /// Creates a new test role instance.
    /// </summary>
    /// <param name="roleNamePrefix">Optional name prefix, name will be randomized.</param>
    /// <param name="useRoleNamePrefixAsRoleName">If true, the prefix should be used as the rolename without a random pad.</param>
    /// <returns></returns>
    protected abstract TRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false);

    /// <summary>
    /// Query used to do name equality checks.
    /// </summary>
    /// <param name="roleName">The role name to match.</param>
    /// <returns>The query to use.</returns>
    protected abstract Expression<Func<TRole, bool>> RoleNameEqualsPredicate(string roleName);

    /// <summary>
    /// Query used to do user name prefix matching.
    /// </summary>
    /// <param name="roleName">The role name to match.</param>
    /// <returns>The query to use.</returns>
    protected abstract Expression<Func<TRole, bool>> RoleNameStartsWithPredicate(string roleName);

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanCreateRoleTest()
    {
        var manager = CreateRoleManager();
        var roleName = "create" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        Assert.False(await manager.RoleExistsAsync(roleName));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        Assert.True(await manager.RoleExistsAsync(roleName));
    }

    private sealed class AlwaysBadValidator : IUserValidator<TUser>, IRoleValidator<TRole>,
        IPasswordValidator<TUser>
    {
        public static readonly IdentityError ErrorMessage = new IdentityError { Description = "I'm Bad.", Code = "BadValidator" };

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            return Task.FromResult(IdentityResult.Failed(ErrorMessage));
        }

        public Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role)
        {
            return Task.FromResult(IdentityResult.Failed(ErrorMessage));
        }

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(IdentityResult.Failed(ErrorMessage));
        }
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task BadValidatorBlocksCreateRole()
    {
        var manager = CreateRoleManager();
        manager.RoleValidators.Clear();
        manager.RoleValidators.Add(new AlwaysBadValidator());
        var role = CreateTestRole("blocked");
        IdentityResultAssert.IsFailure(await manager.CreateAsync(role),
            AlwaysBadValidator.ErrorMessage);
        IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Role {await manager.GetRoleIdAsync(role) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanChainRoleValidators()
    {
        var manager = CreateRoleManager();
        manager.RoleValidators.Clear();
        manager.RoleValidators.Add(new AlwaysBadValidator());
        manager.RoleValidators.Add(new AlwaysBadValidator());
        var role = CreateTestRole("blocked");
        var result = await manager.CreateAsync(role);
        IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
        IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Role {await manager.GetRoleIdAsync(role) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code};{AlwaysBadValidator.ErrorMessage.Code}.");
        Assert.Equal(2, result.Errors.Count());
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task BadValidatorBlocksRoleUpdate()
    {
        var manager = CreateRoleManager();
        var role = CreateTestRole("poorguy");
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        var error = AlwaysBadValidator.ErrorMessage;
        manager.RoleValidators.Clear();
        manager.RoleValidators.Add(new AlwaysBadValidator());
        IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
        IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Role {await manager.GetRoleIdAsync(role) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanDeleteRole()
    {
        var manager = CreateRoleManager();
        var roleName = "delete" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        Assert.False(await manager.RoleExistsAsync(roleName));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        Assert.True(await manager.RoleExistsAsync(roleName));
        IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
        Assert.False(await manager.RoleExistsAsync(roleName));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanAddRemoveRoleClaim()
    {
        var manager = CreateRoleManager();
        var role = CreateTestRole("ClaimsAddRemove");
        var roleSafe = CreateTestRole("ClaimsAdd");
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(roleSafe));
        Claim[] claims = [new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3")];
        foreach (Claim c in claims)
        {
            IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(role, c));
            IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(roleSafe, c));
        }
        var roleClaims = await manager.GetClaimsAsync(role);
        var safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
        Assert.Equal(3, roleClaims.Count);
        Assert.Equal(3, safeRoleClaims.Count);
        IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[0]));
        roleClaims = await manager.GetClaimsAsync(role);
        safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
        Assert.Equal(2, roleClaims.Count);
        Assert.Equal(3, safeRoleClaims.Count);
        IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[1]));
        roleClaims = await manager.GetClaimsAsync(role);
        safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
        Assert.Single(roleClaims);
        Assert.Equal(3, safeRoleClaims.Count);
        IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[2]));
        roleClaims = await manager.GetClaimsAsync(role);
        safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
        Assert.Empty(roleClaims);
        Assert.Equal(3, safeRoleClaims.Count);
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanRoleFindById()
    {
        var manager = CreateRoleManager();
        var role = CreateTestRole("FindByIdAsync");
        Assert.Null(await manager.FindByIdAsync(await manager.GetRoleIdAsync(role)));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        Assert.Equal(role, await manager.FindByIdAsync(await manager.GetRoleIdAsync(role)));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanRoleFindByName()
    {
        var manager = CreateRoleManager();
        var roleName = "FindByNameAsync" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        Assert.Null(await manager.FindByNameAsync(roleName));
        Assert.False(await manager.RoleExistsAsync(roleName));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        Assert.Equal(role, await manager.FindByNameAsync(roleName));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanUpdateRoleName()
    {
        var manager = CreateRoleManager();
        var roleName = "update" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        Assert.False(await manager.RoleExistsAsync(roleName));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        Assert.True(await manager.RoleExistsAsync(roleName));
        IdentityResultAssert.IsSuccess(await manager.SetRoleNameAsync(role, "Changed"));
        IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
        Assert.False(await manager.RoleExistsAsync("update"));
        Assert.Equal(role, await manager.FindByNameAsync("Changed"));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public virtual async Task CanQueryableRoles()
    {
        var manager = CreateRoleManager();
        if (manager.SupportsQueryableRoles)
        {
            var roles = GenerateRoles("CanQueryableRolesTest", 4);
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
            }
            Expression<Func<TRole, bool>> func = RoleNameStartsWithPredicate("CanQueryableRolesTest");
            Assert.Equal(roles.Count, manager.Roles.Count(func));
            func = RoleNameEqualsPredicate("bogus");
            Assert.Empty(manager.Roles.Where(func));

        }
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CreateRoleFailsIfExists()
    {
        var manager = CreateRoleManager();
        var roleName = "dupeRole" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        Assert.False(await manager.RoleExistsAsync(roleName));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        Assert.True(await manager.RoleExistsAsync(roleName));
        var role2 = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanAddUsersToRole()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        var roleManager = CreateRoleManager(context);
        var roleName = "AddUserTest" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
        TUser[] users =
        [
                CreateTestUser("1"),CreateTestUser("2"),CreateTestUser("3"),CreateTestUser("4"),
            ];
        foreach (var u in users)
        {
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(u, roleName));
            Assert.True(await manager.IsInRoleAsync(u, roleName));
        }
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanGetRolesForUser()
    {
        var context = CreateTestContext();
        var userManager = CreateManager(context);
        var roleManager = CreateRoleManager(context);
        var users = GenerateUsers("CanGetRolesForUser", 4);
        var roles = GenerateRoles("CanGetRolesForUserRole", 4);
        foreach (var u in users)
        {
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
        }
        foreach (var r in roles)
        {
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
                Assert.True(await userManager.IsInRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
            }
        }

        foreach (var u in users)
        {
            var rs = await userManager.GetRolesAsync(u);
            Assert.Equal(roles.Count, rs.Count);
            foreach (var r in roles)
            {
                var expectedRoleName = await roleManager.GetRoleNameAsync(r);
                Assert.Contains(rs, role => role == expectedRoleName);
            }
        }
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task RemoveUserFromRoleWithMultipleRoles()
    {
        var context = CreateTestContext();
        var userManager = CreateManager(context);
        var roleManager = CreateRoleManager(context);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
        var roles = GenerateRoles("RemoveUserFromRoleWithMultipleRoles", 4);
        foreach (var r in roles)
        {
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
            IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, await roleManager.GetRoleNameAsync(r)));
            Assert.True(await userManager.IsInRoleAsync(user, await roleManager.GetRoleNameAsync(r)));
        }
        IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(user, await roleManager.GetRoleNameAsync(roles[2])));
        Assert.False(await userManager.IsInRoleAsync(user, await roleManager.GetRoleNameAsync(roles[2])));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanRemoveUsersFromRole()
    {
        var context = CreateTestContext();
        var userManager = CreateManager(context);
        var roleManager = CreateRoleManager(context);
        var users = GenerateUsers("CanRemoveUsersFromRole", 4);
        foreach (var u in users)
        {
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
        }
        var r = CreateTestRole("r1");
        var roleName = await roleManager.GetRoleNameAsync(r);
        IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
        foreach (var u in users)
        {
            IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, roleName));
            Assert.True(await userManager.IsInRoleAsync(u, roleName));
        }
        foreach (var u in users)
        {
            IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(u, roleName));
            Assert.False(await userManager.IsInRoleAsync(u, roleName));
        }
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task RemoveUserNotInRoleFails()
    {
        var context = CreateTestContext();
        var userMgr = CreateManager(context);
        var roleMgr = CreateRoleManager(context);
        var roleName = "addUserDupeTest" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        var result = await userMgr.RemoveFromRoleAsync(user, roleName);
        IdentityResultAssert.IsFailure(result, _errorDescriber.UserNotInRole(roleName));
        IdentityResultAssert.VerifyLogMessage(userMgr.Logger, $"User is not in role {roleName}.");
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task AddUserToRoleFailsIfAlreadyInRole()
    {
        var context = CreateTestContext();
        var userMgr = CreateManager(context);
        var roleMgr = CreateRoleManager(context);
        var roleName = "addUserDupeTest" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));
        Assert.True(await userMgr.IsInRoleAsync(user, roleName));
        IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user, roleName), _errorDescriber.UserAlreadyInRole(roleName));
        IdentityResultAssert.VerifyLogMessage(userMgr.Logger, $"User is already in role {roleName}.");
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task AddUserToRolesIgnoresDuplicates()
    {
        var context = CreateTestContext();
        var userMgr = CreateManager(context);
        var roleMgr = CreateRoleManager(context);
        var roleName = "addUserDupeTest" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        Assert.False(await userMgr.IsInRoleAsync(user, roleName));
        IdentityResultAssert.IsSuccess(await userMgr.AddToRolesAsync(user, new[] { roleName, roleName }));
        Assert.True(await userMgr.IsInRoleAsync(user, roleName));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanFindRoleByNameWithManager()
    {
        var roleMgr = CreateRoleManager();
        var roleName = "findRoleByNameTest" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        Assert.NotNull(await roleMgr.FindByNameAsync(roleName));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanFindRoleWithManager()
    {
        var roleMgr = CreateRoleManager();
        var roleName = "findRoleTest" + Guid.NewGuid().ToString();
        var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
        IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        Assert.Equal(roleName, await roleMgr.GetRoleNameAsync(await roleMgr.FindByNameAsync(roleName)));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanGetUsersInRole()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        var roleManager = CreateRoleManager(context);
        var roles = GenerateRoles("UsersInRole", 4);
        var roleNameList = new List<string>();

        foreach (var role in roles)
        {
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
            roleNameList.Add(await roleManager.GetRoleNameAsync(role));
        }

        for (int i = 0; i < 6; i++)
        {
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

            if ((i % 2) == 0)
            {
                IdentityResultAssert.IsSuccess(await manager.AddToRolesAsync(user, roleNameList));
            }
        }

        foreach (var role in roles)
        {
            Assert.Equal(3, (await manager.GetUsersInRoleAsync(await roleManager.GetRoleNameAsync(role))).Count);
        }

        Assert.Empty((await manager.GetUsersInRoleAsync("123456")));
    }

    private List<TRole> GenerateRoles(string namePrefix, int count)
    {
        var roles = new List<TRole>(count);
        for (var i = 0; i < count; i++)
        {
            roles.Add(CreateTestRole(namePrefix + i));
        }
        return roles;
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanAddAndRetrievePasskey()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        Assert.True(manager.SupportsUserPasskey);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var credentialId = Guid.NewGuid().ToByteArray();
        var passkey = new UserPasskeyInfo(
            credentialId,
            publicKey: [1, 2, 3, 4],
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: ["usb"],
            isUserVerified: false,
            isBackupEligible: true,
            isBackedUp: false,
            attestationObject: [5, 6, 7],
            clientDataJson: [8, 9])
        {
            Name = "InitialName"
        };

        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, passkey));

        var fetched = await manager.GetPasskeyAsync(user, credentialId);
        Assert.NotNull(fetched);
        Assert.Equal("InitialName", fetched.Name);
        Assert.Equal(0u, fetched.SignCount);

        var list = await manager.GetPasskeysAsync(user);
        Assert.Single(list);
        Assert.Equal("InitialName", list[0].Name);
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task AddOrUpdatePasskeyUpdatesMutableFields()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        Assert.True(manager.SupportsUserPasskey);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var credentialId = Guid.NewGuid().ToByteArray();
        var passkey = new UserPasskeyInfo(
            credentialId,
            publicKey: [10, 11, 12],
            DateTimeOffset.UtcNow,
            signCount: 1,
            transports: ["nfc"],
            isUserVerified: false,
            isBackupEligible: true,
            isBackedUp: false,
            attestationObject: [13],
            clientDataJson: [14])
        {
            Name = "Original"
        };

        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, passkey));

        passkey.Name = "Updated";
        passkey.SignCount = 2;
        passkey.IsBackedUp = true;
        passkey.IsUserVerified = true;

        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, passkey));

        var updated = await manager.GetPasskeyAsync(user, credentialId);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(2u, updated.SignCount);
        Assert.True(updated.IsBackedUp);
        Assert.True(updated.IsUserVerified);
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanRemovePasskey()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        Assert.True(manager.SupportsUserPasskey);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var passkey = new UserPasskeyInfo(
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: [1],
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: null,
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            attestationObject: [2],
            clientDataJson: [3])
        {
            Name = "ToRemove"
        };

        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, passkey));
        Assert.Single(await manager.GetPasskeysAsync(user));
        IdentityResultAssert.IsSuccess(await manager.RemovePasskeyAsync(user, passkey.CredentialId));
        Assert.Empty(await manager.GetPasskeysAsync(user));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CanAddMultiplePasskeys()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        Assert.True(manager.SupportsUserPasskey);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var passkey1 = new UserPasskeyInfo(
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: [1],
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: ["usb"],
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            attestationObject: [10],
            clientDataJson: [11])
        {
            Name = "One"
        };
        var pk2 = new UserPasskeyInfo(
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: [2],
            DateTimeOffset.UtcNow,
            signCount: 5,
            transports: ["nfc"],
            isUserVerified: true,
            isBackupEligible: false,
            isBackedUp: false,
            attestationObject: [12],
            clientDataJson: [13])
        {
            Name = "Two"
        };

        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, passkey1));
        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, pk2));

        var all = await manager.GetPasskeysAsync(user);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.Name == "One");
        Assert.Contains(all, p => p.Name == "Two");
        Assert.NotNull(await manager.GetPasskeyAsync(user, passkey1.CredentialId));
        Assert.NotNull(await manager.GetPasskeyAsync(user, pk2.CredentialId));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task CannotAccessAnotherUsersPasskey()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user1));
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));

        var passkey = new UserPasskeyInfo(
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: [1],
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: ["usb"],
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            attestationObject: [10],
            clientDataJson: [11])
        {
            Name = "One"
        };
        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user1, passkey));

        // user2 should not see user1's passkey via GetPasskeyAsync
        Assert.Null(await manager.GetPasskeyAsync(user2, passkey.CredentialId));
        var owner = await manager.FindByPasskeyIdAsync(passkey.CredentialId);
        Assert.Equal(await manager.GetUserIdAsync(user1), await manager.GetUserIdAsync(owner));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task FindByPasskeyIdReturnsCorrectUser()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var passkey = new UserPasskeyInfo(
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: [1],
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: ["usb"],
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            attestationObject: [10],
            clientDataJson: [11])
        {
            Name = "One"
        };
        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, passkey));

        var found = await manager.FindByPasskeyIdAsync(passkey.CredentialId);
        Assert.Equal(await manager.GetUserIdAsync(user), await manager.GetUserIdAsync(found));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task UpdatingPasskeyDoesNotChangeImmutableFields()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var original = new UserPasskeyInfo(
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: [9, 9],
            createdAt: DateTimeOffset.UtcNow,
            signCount: 1,
            transports: ["usb", "nfc"],
            isUserVerified: false,
            isBackupEligible: true,
            isBackedUp: false,
            attestationObject: [5],
            clientDataJson: [6])
        {
            Name = "ImmutableTest"
        };
        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, original));

        // Attempt to modify immutable fields
        var update = new UserPasskeyInfo(
            credentialId: original.CredentialId,
            publicKey: [0xFF, 0xFF],
            createdAt: original.CreatedAt.AddMinutes(5),
            signCount: 3,
            transports: ["ble"],
            isUserVerified: true,
            isBackupEligible: false,
            isBackedUp: true,
            attestationObject: [7],
            clientDataJson: [8])
        {
            Name = "Changed"
        };

        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, update));

        var stored = await manager.GetPasskeyAsync(user, original.CredentialId);
        Assert.NotNull(stored);

        // Mutable
        Assert.Equal("Changed", stored.Name);
        Assert.Equal((uint)3, stored.SignCount);
        Assert.True(stored.IsBackedUp);
        Assert.True(stored.IsUserVerified);

        // Immutable
        Assert.Equal(original.PublicKey, stored.PublicKey);
        Assert.Equal(original.CreatedAt, stored.CreatedAt);
        Assert.Equal(original.IsBackupEligible, stored.IsBackupEligible);
        Assert.Equal(original.AttestationObject, stored.AttestationObject);
        Assert.Equal(original.ClientDataJson, stored.ClientDataJson);
        Assert.Equal(original.Transports, stored.Transports);
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task RemovePasskeyIsIdempotent()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var pk = new UserPasskeyInfo(Guid.NewGuid().ToByteArray(), [1], DateTimeOffset.UtcNow, 0, null, false, false, false, [2], [3]);
        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, pk));

        IdentityResultAssert.IsSuccess(await manager.RemovePasskeyAsync(user, pk.CredentialId));
        Assert.Empty(await manager.GetPasskeysAsync(user));

        // Second removal should not throw or change anything
        IdentityResultAssert.IsSuccess(await manager.RemovePasskeyAsync(user, pk.CredentialId));
        Assert.Empty(await manager.GetPasskeysAsync(user));
    }

    /// <summary>
    /// Test.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task FindPasskeyReturnsNullForUnknownCredential()
    {
        var context = CreateTestContext();
        var manager = CreateManager(context);
        var user = CreateTestUser();
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

        var pk = new UserPasskeyInfo(Guid.NewGuid().ToByteArray(), [7], DateTimeOffset.UtcNow, 0, null, false, false, false, [8], [9]);
        IdentityResultAssert.IsSuccess(await manager.AddOrUpdatePasskeyAsync(user, pk));

        var randomId = Guid.NewGuid().ToByteArray();
        Assert.Null(await manager.GetPasskeyAsync(user, randomId));
    }
}
