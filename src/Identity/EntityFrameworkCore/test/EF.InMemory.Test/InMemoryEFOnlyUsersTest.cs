// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public class InMemoryEFOnlyUsersTest
    : UserManagerSpecificationTestBase<IdentityUser, string>,
        IClassFixture<InMemoryDatabaseFixture>
{
    private readonly InMemoryDatabaseFixture _fixture;

    public InMemoryEFOnlyUsersTest(InMemoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    protected override object CreateTestContext()
        => InMemoryContext<IdentityUser>.Create(_fixture.Connection);

    protected override void AddUserStore(IServiceCollection services, object context = null)
        => services.AddSingleton<IUserStore<IdentityUser>>(new UserStore<IdentityUser, IdentityRole, DbContext, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, IdentityUserToken<string>, IdentityRoleClaim<string>>((InMemoryContext<IdentityUser>)context, new IdentityErrorDescriber()));

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

    protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
    {
        user.PasswordHash = hashedPassword;
    }

    protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

#pragma warning disable CA1310 // Specify StringComparison for correctness
    protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);
#pragma warning restore CA1310 // Specify StringComparison for correctness
}
