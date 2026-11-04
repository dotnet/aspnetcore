// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public class InMemoryEFUserStoreTest : IdentitySpecificationTestBase<IdentityUser, IdentityRole, string>, IClassFixture<InMemoryDatabaseFixture>
{
    private readonly InMemoryDatabaseFixture _fixture;

    public InMemoryEFUserStoreTest(InMemoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    protected override object CreateTestContext()
        => InMemoryContext.Create(_fixture.Connection);

    protected override void AddUserStore(IServiceCollection services, object context = null)
    {
        services.AddSingleton<IUserStore<IdentityUser>>(new UserStore<IdentityUser>((InMemoryContext)context));
    }

    protected override void AddRoleStore(IServiceCollection services, object context = null)
    {
        var store = new RoleStore<IdentityRole, InMemoryContext>((InMemoryContext)context);
        services.AddSingleton<IRoleStore<IdentityRole>>(store);
    }

    protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
        bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?), bool useNamePrefixAsUserName = false)
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

    protected override IdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
    {
        var roleName = useRoleNamePrefixAsRoleName ? roleNamePrefix : string.Format(CultureInfo.InvariantCulture, "{0}{1}", roleNamePrefix, Guid.NewGuid());
        return new IdentityRole(roleName);
    }

    protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
    {
        user.PasswordHash = hashedPassword;
    }

    protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

    protected override Expression<Func<IdentityRole, bool>> RoleNameEqualsPredicate(string roleName) => r => r.Name == roleName;

#pragma warning disable CA1310 // Specify StringComparison for correctness
    protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

    protected override Expression<Func<IdentityRole, bool>> RoleNameStartsWithPredicate(string roleName) => r => r.Name.StartsWith(roleName);
#pragma warning restore CA1310 // Specify StringComparison for correctness
}
