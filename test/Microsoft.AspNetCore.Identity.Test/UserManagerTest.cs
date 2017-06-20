// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class UserManagerTest
    {
        [Fact]
        public void EnsureDefaultServicesDefaultsWithStoreWorks()
        {
            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(config)
                    .AddTransient<IUserStore<TestUser>, NoopUserStore>();
            services.AddIdentity<TestUser, TestRole>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLogging();
            var manager = services.BuildServiceProvider().GetRequiredService<UserManager<TestUser>>();
            Assert.NotNull(manager.PasswordHasher);
            Assert.NotNull(manager.Options);
        }

        [Fact]
        public void AddUserManagerWithCustomManagerReturnsSameInstance()
        {
            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(config)
                    .AddTransient<IUserStore<TestUser>, NoopUserStore>()
                    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddLogging();

            services.AddIdentity<TestUser, TestRole>()
                .AddUserManager<CustomUserManager>()
                .AddRoleManager<CustomRoleManager>();
            var provider = services.BuildServiceProvider();
            Assert.Same(provider.GetRequiredService<UserManager<TestUser>>(),
                provider.GetRequiredService<CustomUserManager>());
            Assert.Same(provider.GetRequiredService<RoleManager<TestRole>>(),
                provider.GetRequiredService<CustomRoleManager>());
        }

        public class CustomUserManager : UserManager<TestUser>
        {
            public CustomUserManager() : base(new Mock<IUserStore<TestUser>>().Object, null, null, null, null, null, null, null, null)
            { }
        }

        public class CustomRoleManager : RoleManager<TestRole>
        {
            public CustomRoleManager() : base(new Mock<IRoleStore<TestRole>>().Object, null, null, null, null)
            { }
        }

        [Fact]
        public async Task CreateCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.CreateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
            store.Setup(s => s.SetNormalizedUserNameAsync(user, user.UserName.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

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
            var store = new Mock<IUserEmailStore<TestUser>>();
            var user = new TestUser { UserName = "Foo", Email = "Foo@foo.com" };
            store.Setup(s => s.CreateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
            store.Setup(s => s.GetEmailAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.Email)).Verifiable();
            store.Setup(s => s.SetNormalizedEmailAsync(user, user.Email.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(s => s.SetNormalizedUserNameAsync(user, user.UserName.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

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
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
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
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
            store.Setup(s => s.SetNormalizedUserNameAsync(user, user.UserName.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
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
            var store = new Mock<IUserEmailStore<TestUser>>();
            var user = new TestUser { UserName = "Foo", Email = "email" };
            store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.UserName)).Verifiable();
            store.Setup(s => s.GetEmailAsync(user, CancellationToken.None)).Returns(Task.FromResult(user.Email)).Verifiable();
            store.Setup(s => s.SetNormalizedUserNameAsync(user, user.UserName.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(s => s.SetNormalizedEmailAsync(user, user.Email.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
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
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser();
            store.Setup(s => s.SetUserNameAsync(user, "foo", CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).Returns(Task.FromResult("foo")).Verifiable();
            store.Setup(s => s.SetNormalizedUserNameAsync(user, "FOO", CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
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
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.FindByIdAsync(user.Id, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

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
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.FindByNameAsync(user.UserName.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

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
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
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
            var store = new Mock<IUserEmailStore<TestUser>>();
            var user = new TestUser { Email = "Foo" };
            store.Setup(s => s.FindByEmailAsync(user.Email.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
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
            var store = new Mock<IUserEmailStore<TestUser>>();
            var user = new TestUser { Email = "Foo" };
            store.Setup(s => s.FindByEmailAsync(user.Email, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = MockHelpers.TestUserManager(store.Object);
            userManager.KeyNormalizer = null;

            // Act
            var result = await userManager.FindByEmailAsync(user.Email);

            // Assert
            Assert.Equal(user, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task AddToRolesCallsStore()
        {
            // Setup
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new string[] { "A", "B", "C", "C" };
            store.Setup(s => s.AddToRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.AddToRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.AddToRoleAsync(user, "C", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();

            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(false))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(false))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "C", CancellationToken.None))
                .Returns(Task.FromResult(false))
                .Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.AddToRolesAsync(user, roles);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
            store.Verify(s => s.AddToRoleAsync(user, "C", CancellationToken.None), Times.Once());
        }

        [Fact]
        public async Task AddToRolesFailsIfUserInRole()
        {
            // Setup
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new[] { "A", "B", "C" };
            store.Setup(s => s.AddToRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "B", CancellationToken.None))
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
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new[] { "A", "B", "C" };
            store.Setup(s => s.RemoveFromRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.RemoveFromRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.RemoveFromRoleAsync(user, "C", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "C", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

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
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new string[] { "A", "B", "C" };
            store.Setup(s => s.RemoveFromRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(false))
                .Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

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
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
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
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
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
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
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
            var store = new Mock<IUserPasswordStore<TestUser>>();
            var hasher = new Mock<IPasswordHasher<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var pwd = "password";
            var hashed = "hashed";
            var rehashed = "rehashed";

            store.Setup(s => s.GetPasswordHashAsync(user, CancellationToken.None))
                .ReturnsAsync(hashed)
                .Verifiable();
            store.Setup(s => s.SetPasswordHashAsync(user, It.IsAny<string>(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(x => x.UpdateAsync(It.IsAny<TestUser>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(IdentityResult.Success));

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
            var store = new Mock<IUserSecurityStampStore<TestUser>>();
            var manager = MockHelpers.TestUserManager(store.Object);
            var user = new TestUser { UserName = "nulldude" };
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
            var store = new Mock<IUserSecurityStampStore<TestUser>>();
            var manager = MockHelpers.TestUserManager(store.Object);
            var user = new TestUser { UserName = "nulldude" };
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
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
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
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var claim = new Claim("1", "1");
            store.Setup(s => s.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>(), CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

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
            var user = new TestUser();
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
            var store = new Mock<IUserStore<TestUser>>();
            store.Setup(x => x.GetUserIdAsync(It.IsAny<TestUser>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Guid.NewGuid().ToString()));
            var manager = MockHelpers.TestUserManager(store.Object);
            Assert.False(manager.SupportsUserSecurityStamp);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.UpdateSecurityStampAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetSecurityStampAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.VerifyChangePhoneNumberTokenAsync(new TestUser(), "1", "111-111-1111"));
            await Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.GenerateChangePhoneNumberTokenAsync(new TestUser(), "111-111-1111"));
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

        private class ATokenProvider : IUserTwoFactorTokenProvider<TestUser>
        {
            public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TestUser> manager, TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<string> GenerateAsync(string purpose, UserManager<TestUser> manager, TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<TestUser> manager, TestUser user)
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

            services.AddIdentity<TestUser, TestRole>(o => o.Tokens.ProviderMap.Add("A", new TokenProviderDescriptor(typeof(ATokenProvider))
            {
                ProviderInstance = provider
            })).AddUserStore<NoopUserStore>();
            var manager = services.BuildServiceProvider().GetService<UserManager<TestUser>>();
            Assert.ThrowsAsync<NotImplementedException>(() => manager.GenerateUserTokenAsync(new TestUser(), "A", "purpose"));
        }

        [Fact]
        public void UserManagerWillUseTokenProviderInstanceOverDefaults()
        {
            var provider = new ATokenProvider();
            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(config)
                    .AddLogging();

            services.AddIdentity<TestUser, TestRole>(o => o.Tokens.ProviderMap.Add(TokenOptions.DefaultProvider, new TokenProviderDescriptor(typeof(ATokenProvider))
                {
                    ProviderInstance = provider
                })).AddUserStore<NoopUserStore>().AddDefaultTokenProviders();
            var manager = services.BuildServiceProvider().GetService<UserManager<TestUser>>();
            Assert.ThrowsAsync<NotImplementedException>(() => manager.GenerateUserTokenAsync(new TestUser(), TokenOptions.DefaultProvider, "purpose"));
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
            // TODO: Can switch to Mock eventually
            var manager = MockHelpers.TestUserManager(new EmptyStore());
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new BadPasswordValidator<TestUser>());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(new TestUser(), "password"),
                BadPasswordValidator<TestUser>.ErrorMessage);
        }

        [Fact]
        public async Task ResetTokenCallNoopForTokenValueZero()
        {
            var user = new TestUser() { UserName = Guid.NewGuid().ToString() };
            var store = new Mock<IUserLockoutStore<TestUser>>();
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
                () => new UserManager<TestUser>(null, null, null, null, null, null, null, null, null));

            var manager = MockHelpers.TestUserManager(new NotImplementedStore());

            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("password",
                    async () => await manager.CreateAsync(new TestUser(), null));
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
            await Assert.ThrowsAsync<ArgumentNullException>("roles", async () => await manager.AddToRolesAsync(new TestUser(), null));
            await Assert.ThrowsAsync<ArgumentNullException>("roles", async () => await manager.RemoveFromRolesAsync(new TestUser(), null));
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

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }
        }

        private class EmptyStore :
            IUserPasswordStore<TestUser>,
            IUserClaimStore<TestUser>,
            IUserLoginStore<TestUser>,
            IUserEmailStore<TestUser>,
            IUserPhoneNumberStore<TestUser>,
            IUserLockoutStore<TestUser>,
            IUserTwoFactorStore<TestUser>,
            IUserRoleStore<TestUser>,
            IUserSecurityStampStore<TestUser>
        {
            public Task<IList<Claim>> GetClaimsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<Claim>>(new List<Claim>());
            }

            public Task AddClaimsAsync(TestUser user, IEnumerable<Claim> claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task ReplaceClaimAsync(TestUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveClaimsAsync(TestUser user, IEnumerable<Claim> claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task SetEmailAsync(TestUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetEmailAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task<bool> GetEmailConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetEmailConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<DateTimeOffset?> GetLockoutEndDateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<DateTimeOffset?>(DateTimeOffset.MinValue);
            }

            public Task SetLockoutEndDateAsync(TestUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<int> IncrementAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task ResetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<int> GetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetLockoutEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetLockoutEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task AddLoginAsync(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveLoginAsync(TestUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<UserLoginInfo>> GetLoginsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>());
            }

            public Task<TestUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public void Dispose()
            {
            }

            public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IdentityResult> CreateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(IdentityResult.Success);
            }

            public Task<IdentityResult> UpdateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(IdentityResult.Success);
            }

            public Task<IdentityResult> DeleteAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(IdentityResult.Success);
            }

            public Task<TestUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<TestUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task SetPasswordHashAsync(TestUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPasswordHashAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<bool> HasPasswordAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetPhoneNumberAsync(TestUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPhoneNumberAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task<bool> GetPhoneNumberConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetPhoneNumberConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task AddToRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveFromRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<string>> GetRolesAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<string>>(new List<string>());
            }

            public Task<bool> IsInRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetSecurityStampAsync(TestUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetSecurityStampAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task SetTwoFactorEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetTwoFactorEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<string> GetUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<string> GetNormalizedUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task SetNormalizedUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<TestUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<TestUser>>(new List<TestUser>());
            }

            public Task<IList<TestUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<TestUser>>(new List<TestUser>());
            }

            public Task<string> GetNormalizedEmailAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task SetNormalizedEmailAsync(TestUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }
        }

        private class NoOpTokenProvider : IUserTwoFactorTokenProvider<TestUser>
        {
            public string Name { get; } = "Noop";

            public Task<string> GenerateAsync(string purpose, UserManager<TestUser> manager, TestUser user)
            {
                return Task.FromResult("Test");
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<TestUser> manager, TestUser user)
            {
                return Task.FromResult(true);
            }

            public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TestUser> manager, TestUser user)
            {
                return Task.FromResult(true);
            }
        }

        private class NotImplementedStore :
            IUserPasswordStore<TestUser>,
            IUserClaimStore<TestUser>,
            IUserLoginStore<TestUser>,
            IUserRoleStore<TestUser>,
            IUserEmailStore<TestUser>,
            IUserPhoneNumberStore<TestUser>,
            IUserLockoutStore<TestUser>,
            IUserTwoFactorStore<TestUser>
        {
            public Task<IList<Claim>> GetClaimsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddClaimsAsync(TestUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task ReplaceClaimAsync(TestUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveClaimsAsync(TestUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetEmailAsync(TestUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetEmailAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetEmailConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetEmailConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<DateTimeOffset?> GetLockoutEndDateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEndDateAsync(TestUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<int> IncrementAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task ResetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<int> GetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetLockoutEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddLoginAsync(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveLoginAsync(TestUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<UserLoginInfo>> GetLoginsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPasswordHashAsync(TestUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPasswordHashAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> HasPasswordAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumberAsync(TestUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPhoneNumberAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetPhoneNumberConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumberConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetTwoFactorEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetTwoFactorEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddToRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveFromRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<string>> GetRolesAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> IsInRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<TestUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<TestUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            Task<IdentityResult> IUserStore<TestUser>.CreateAsync(TestUser user, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            Task<IdentityResult> IUserStore<TestUser>.UpdateAsync(TestUser user, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            Task<IdentityResult> IUserStore<TestUser>.DeleteAsync(TestUser user, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedEmailAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedEmailAsync(TestUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task CanCustomizeUserValidatorErrors()
        {
            var store = new Mock<IUserEmailStore<TestUser>>();
            var describer = new TestErrorDescriber();
            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(config)
                    .AddLogging()
                    .AddSingleton<IdentityErrorDescriber>(describer)
                    .AddSingleton<IUserStore<TestUser>>(store.Object)
                    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddIdentity<TestUser, TestRole>();

            var manager = services.BuildServiceProvider().GetRequiredService<UserManager<TestUser>>();

            manager.Options.User.RequireUniqueEmail = true;
            var user = new TestUser() { UserName = "dupeEmail", Email = "dupe@email.com" };
            var user2 = new TestUser() { UserName = "dupeEmail2", Email = "dupe@email.com" };
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
                return new IdentityError { Code = Code, Description = string.Format(FormatError, email) };
            }
        }

    }
}