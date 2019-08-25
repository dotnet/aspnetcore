// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
    public class InMemoryNonIdentityDerivedEFUserStoreWithGenericsTest
    : IdentitySpecificationTestBase<IdentityUserWithGenerics, MyIdentityRole, string>, IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;
        private readonly InMemoryNonIdentityDerivedContextWithGenerics _context;
        private UserStoreWithNonIdentityDerivedDbContextAndGenerics _store;

        public InMemoryNonIdentityDerivedEFUserStoreWithGenericsTest(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;

            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddDbContext<InMemoryNonIdentityDerivedContextWithGenerics>(
                options => options
                    .UseSqlite(_fixture.Connection)
                    .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)));
            _context = services.BuildServiceProvider().GetRequiredService<InMemoryNonIdentityDerivedContextWithGenerics>();

            _context.Database.EnsureCreated();
        }

        protected override object CreateTestContext()
        {
            return _context;
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            _store = new UserStoreWithNonIdentityDerivedDbContextAndGenerics((InMemoryNonIdentityDerivedContextWithGenerics)context, "TestContext");
            services.AddSingleton<IUserStore<IdentityUserWithGenerics>>(_store);
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IRoleStore<MyIdentityRole>>(new RoleStoreWithNonIdentityDerivedDbContextAndGenerics((InMemoryNonIdentityDerivedContextWithGenerics)context, "TestContext"));
        }

        protected override IdentityUserWithGenerics CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?), bool useNamePrefixAsUserName = false)
        {
            return new IdentityUserWithGenerics
            {
                UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
                Email = email,
                PhoneNumber = phoneNumber,
                LockoutEnabled = lockoutEnabled,
                LockoutEnd = lockoutEnd
            };
        }

        protected override MyIdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
        {
            var roleName = useRoleNamePrefixAsRoleName ? roleNamePrefix : string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
            return new MyIdentityRole(roleName);
        }

        protected override void SetUserPasswordHash(IdentityUserWithGenerics user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        protected override Expression<Func<IdentityUserWithGenerics, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

        protected override Expression<Func<MyIdentityRole, bool>> RoleNameEqualsPredicate(string roleName) => r => r.Name == roleName;

        protected override Expression<Func<IdentityUserWithGenerics, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

        protected override Expression<Func<MyIdentityRole, bool>> RoleNameStartsWithPredicate(string roleName) => r => r.Name.StartsWith(roleName);

        [Fact]
        public async Task CanAddRemoveUserClaimWithIssuer()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Claim[] claims = { new Claim("c1", "v1", null, "i1"), new Claim("c2", "v2", null, "i2"), new Claim("c2", "v3", null, "i3") };
            foreach (Claim c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
            }

            var userId = await manager.GetUserIdAsync(user);
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(3, userClaims.Count);
            Assert.Equal(3, userClaims.Intersect(claims, ClaimEqualityComparer.Default).Count());

            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(2, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(0, userClaims.Count);
        }

        [Fact]
        public async Task RemoveClaimWithIssuerOnlyAffectsUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var user2 = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
            Claim[] claims = { new Claim("c", "v", null, "i1"), new Claim("c2", "v2", null, "i2"), new Claim("c2", "v3", null, "i3") };
            foreach (Claim c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user2, c));
            }
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(3, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(2, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(0, userClaims.Count);
            var userClaims2 = await manager.GetClaimsAsync(user2);
            Assert.Equal(3, userClaims2.Count);
        }

        [Fact]
        public async Task CanReplaceUserClaimWithIssuer()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("c", "a", "i")));
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            Claim claim = new Claim("c", "b", "i");
            Claim oldClaim = userClaims.FirstOrDefault();
            IdentityResultAssert.IsSuccess(await manager.ReplaceClaimAsync(user, oldClaim, claim));
            var newUserClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, newUserClaims.Count);
            Claim newClaim = newUserClaims.FirstOrDefault();
            Assert.Equal(claim.Type, newClaim.Type);
            Assert.Equal(claim.Value, newClaim.Value);
            Assert.Equal(claim.Issuer, newClaim.Issuer);
        }

        protected class InMemoryNonIdentityDerivedContextWithGenerics : InMemoryNonIdentityDerivedContext<IdentityUserWithGenerics, MyIdentityRole, string, IdentityUserClaimWithIssuer, IdentityUserRoleWithDate, IdentityUserLoginWithContext, IdentityRoleClaimWithIssuer, IdentityUserTokenWithStuff>
        {
            public InMemoryNonIdentityDerivedContextWithGenerics(DbContextOptions<InMemoryNonIdentityDerivedContextWithGenerics> options) : base(options)
            { }
        }

        protected class UserStoreWithNonIdentityDerivedDbContextAndGenerics : UserStore<IdentityUserWithGenerics, MyIdentityRole, InMemoryNonIdentityDerivedContextWithGenerics, string, IdentityUserClaimWithIssuer, IdentityUserRoleWithDate, IdentityUserLoginWithContext, IdentityUserTokenWithStuff, IdentityRoleClaimWithIssuer>
        {
            public string LoginContext { get; set; }

            public UserStoreWithNonIdentityDerivedDbContextAndGenerics(InMemoryNonIdentityDerivedContextWithGenerics context, string loginContext) : base(context)
            {
                LoginContext = loginContext;
            }

            protected override IdentityUserRoleWithDate CreateUserRole(IdentityUserWithGenerics user, MyIdentityRole role)
            {
                return new IdentityUserRoleWithDate()
                {
                    RoleId = role.Id,
                    UserId = user.Id,
                    Created = DateTime.UtcNow
                };
            }

            protected override IdentityUserClaimWithIssuer CreateUserClaim(IdentityUserWithGenerics user, Claim claim)
            {
                return new IdentityUserClaimWithIssuer { UserId = user.Id, ClaimType = claim.Type, ClaimValue = claim.Value, Issuer = claim.Issuer };
            }

            protected override IdentityUserLoginWithContext CreateUserLogin(IdentityUserWithGenerics user, UserLoginInfo login)
            {
                return new IdentityUserLoginWithContext
                {
                    UserId = user.Id,
                    ProviderKey = login.ProviderKey,
                    LoginProvider = login.LoginProvider,
                    ProviderDisplayName = login.ProviderDisplayName,
                    Context = LoginContext
                };
            }

            protected override IdentityUserTokenWithStuff CreateUserToken(IdentityUserWithGenerics user, string loginProvider, string name, string value)
            {
                return new IdentityUserTokenWithStuff
                {
                    UserId = user.Id,
                    LoginProvider = loginProvider,
                    Name = name,
                    Value = value,
                    Stuff = "stuff"
                };
            }
        }

        protected class RoleStoreWithNonIdentityDerivedDbContextAndGenerics : RoleStore<MyIdentityRole, InMemoryNonIdentityDerivedContextWithGenerics, string, IdentityUserRoleWithDate, IdentityRoleClaimWithIssuer>
        {
            private string _loginContext;
            public RoleStoreWithNonIdentityDerivedDbContextAndGenerics(InMemoryNonIdentityDerivedContextWithGenerics context, string loginContext) : base(context)
            {
                _loginContext = loginContext;
            }

        }
    }
}
