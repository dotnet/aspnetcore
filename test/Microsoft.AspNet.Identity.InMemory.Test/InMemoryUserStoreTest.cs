using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class InMemoryStoreTest
    {
        [Fact]
        public async Task DeleteUserTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("Delete");
            UnitTestHelper.IsSuccess(await manager.Create(user));
            UnitTestHelper.IsSuccess(await manager.Delete(user));
            Assert.Null(await manager.FindById(user.Id));
        }

        [Fact]
        public async Task CreateUserNoPasswordTest()
        {
            var manager = CreateManager();
            UnitTestHelper.IsSuccess(await manager.Create(new InMemoryUser("CreateUserTest")));
            var user = await manager.FindByName("CreateUserTest");
            Assert.NotNull(user);
            Assert.Null(user.PasswordHash);
            var logins = await manager.GetLogins(user.Id);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
        }

        [Fact]
        public async Task CreateUserAddLoginTest()
        {
            var manager = CreateManager();
            const string userName = "CreateExternalUserTest";
            const string provider = "ZzAuth";
            const string providerKey = "HaoKey";
            UnitTestHelper.IsSuccess(await manager.Create(new InMemoryUser(userName)));
            var user = await manager.FindByName(userName);
            var login = new UserLoginInfo(provider, providerKey);
            UnitTestHelper.IsSuccess(await manager.AddLogin(user.Id, login));
            var logins = await manager.GetLogins(user.Id);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(provider, logins.First().LoginProvider);
            Assert.Equal(providerKey, logins.First().ProviderKey);
        }

        [Fact]
        public async Task CreateUserLoginAndAddPasswordTest()
        {
            var manager = CreateManager();
            var login = new UserLoginInfo("Provider", "key");
            var user = new InMemoryUser("CreateUserLoginAddPasswordTest");
            UnitTestHelper.IsSuccess(await manager.Create(user));
            UnitTestHelper.IsSuccess(await manager.AddLogin(user.Id, login));
            UnitTestHelper.IsSuccess(await manager.AddPassword(user.Id, "password"));
            var logins = await manager.GetLogins(user.Id);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(user, await manager.Find(login));
            Assert.Equal(user, await manager.Find(user.UserName, "password"));
        }

        [Fact]
        public async Task CreateUserAddRemoveLoginTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("CreateUserAddRemoveLoginTest");
            var login = new UserLoginInfo("Provider", "key");
            var result = await manager.Create(user);
            Assert.NotNull(user);
            UnitTestHelper.IsSuccess(result);
            UnitTestHelper.IsSuccess(await manager.AddLogin(user.Id, login));
            Assert.Equal(user, await manager.Find(login));
            var logins = await manager.GetLogins(user.Id);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(login.LoginProvider, logins.Last().LoginProvider);
            Assert.Equal(login.ProviderKey, logins.Last().ProviderKey);
            var stamp = user.SecurityStamp;
            UnitTestHelper.IsSuccess(await manager.RemoveLogin(user.Id, login));
            Assert.Null(await manager.Find(login));
            logins = await manager.GetLogins(user.Id);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task RemovePasswordTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("RemovePasswordTest");
            const string password = "password";
            UnitTestHelper.IsSuccess(await manager.Create(user, password));
            var stamp = user.SecurityStamp;
            UnitTestHelper.IsSuccess(await manager.RemovePassword(user.Id));
            var u = await manager.FindByName(user.UserName);
            Assert.NotNull(u);
            Assert.Null(u.PasswordHash);
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ChangePasswordTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("ChangePasswordTest");
            const string password = "password";
            const string newPassword = "newpassword";
            UnitTestHelper.IsSuccess(await manager.Create(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            UnitTestHelper.IsSuccess(await manager.ChangePassword(user.Id, password, newPassword));
            Assert.Null(await manager.Find(user.UserName, password));
            Assert.Equal(user, await manager.Find(user.UserName, newPassword));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task AddRemoveUserClaimTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("ClaimsAddRemove");
            UnitTestHelper.IsSuccess(await manager.Create(user));
            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
            foreach (Claim c in claims)
            {
                UnitTestHelper.IsSuccess(await manager.AddClaim(user.Id, c));
            }
            var userClaims = await manager.GetClaims(user.Id);
            Assert.Equal(3, userClaims.Count);
            UnitTestHelper.IsSuccess(await manager.RemoveClaim(user.Id, claims[0]));
            userClaims = await manager.GetClaims(user.Id);
            Assert.Equal(2, userClaims.Count);
            UnitTestHelper.IsSuccess(await manager.RemoveClaim(user.Id, claims[1]));
            userClaims = await manager.GetClaims(user.Id);
            Assert.Equal(1, userClaims.Count);
            UnitTestHelper.IsSuccess(await manager.RemoveClaim(user.Id, claims[2]));
            userClaims = await manager.GetClaims(user.Id);
            Assert.Equal(0, userClaims.Count);
        }

        [Fact]
        public async Task ChangePasswordFallsIfPasswordWrongTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("user");
            UnitTestHelper.IsSuccess(await manager.Create(user, "password"));
            var result = await manager.ChangePassword(user.Id, "bogus", "newpassword");
            UnitTestHelper.IsFailure(result, "Incorrect password.");
        }

        [Fact]
        public async Task AddDupeUserFailsTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("dupe");
            var user2 = new InMemoryUser("dupe");
            UnitTestHelper.IsSuccess(await manager.Create(user));
            UnitTestHelper.IsFailure(await manager.Create(user2), "Name dupe is already taken.");
        }

        [Fact]
        public async Task UpdateSecurityStampTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("stampMe");
            Assert.Null(user.SecurityStamp);
            UnitTestHelper.IsSuccess(await manager.Create(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            UnitTestHelper.IsSuccess(await manager.UpdateSecurityStamp(user.Id));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task AddDupeLoginFailsTest()
        {
            var manager = CreateManager();
            var user = new InMemoryUser("DupeLogin");
            var login = new UserLoginInfo("provder", "key");
            UnitTestHelper.IsSuccess(await manager.Create(user));
            UnitTestHelper.IsSuccess(await manager.AddLogin(user.Id, login));
            var result = await manager.AddLogin(user.Id, login);
            UnitTestHelper.IsFailure(result, "A user with that external login already exists.");
        }

        // Lockout tests

        [Fact]
        public async Task SingleFailureLockout()
        {
            var mgr = CreateManager();
            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.UserLockoutEnabledByDefault = true;
            var user = new InMemoryUser("fastLockout");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOut(user.Id));
            UnitTestHelper.IsSuccess(await mgr.AccessFailed(user.Id));
            Assert.True(await mgr.IsLockedOut(user.Id));
            Assert.True(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCount(user.Id));
        }

        [Fact]
        public async Task TwoFailureLockout()
        {
            var mgr = CreateManager();
            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.UserLockoutEnabledByDefault = true;
            mgr.MaxFailedAccessAttemptsBeforeLockout = 2;
            var user = new InMemoryUser("twoFailureLockout");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOut(user.Id));
            UnitTestHelper.IsSuccess(await mgr.AccessFailed(user.Id));
            Assert.False(await mgr.IsLockedOut(user.Id));
            Assert.False(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCount(user.Id));
            UnitTestHelper.IsSuccess(await mgr.AccessFailed(user.Id));
            Assert.True(await mgr.IsLockedOut(user.Id));
            Assert.True(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCount(user.Id));
        }

        [Fact]
        public async Task ResetLockoutTest()
        {
            var mgr = CreateManager();
            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.UserLockoutEnabledByDefault = true;
            mgr.MaxFailedAccessAttemptsBeforeLockout = 2;
            var user = new InMemoryUser("resetLockout");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOut(user.Id));
            UnitTestHelper.IsSuccess(await mgr.AccessFailed(user.Id));
            Assert.False(await mgr.IsLockedOut(user.Id));
            Assert.False(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCount(user.Id));
            UnitTestHelper.IsSuccess(await mgr.ResetAccessFailedCount(user.Id));
            Assert.Equal(0, await mgr.GetAccessFailedCount(user.Id));
            Assert.False(await mgr.IsLockedOut(user.Id));
            Assert.False(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            UnitTestHelper.IsSuccess(await mgr.AccessFailed(user.Id));
            Assert.False(await mgr.IsLockedOut(user.Id));
            Assert.False(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCount(user.Id));
        }

        [Fact]
        public async Task EnableLockoutManually()
        {
            var mgr = CreateManager();
            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.MaxFailedAccessAttemptsBeforeLockout = 2;
            var user = new InMemoryUser("manualLockout");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.False(await mgr.GetLockoutEnabled(user.Id));
            Assert.False(user.LockoutEnabled);
            UnitTestHelper.IsSuccess(await mgr.SetLockoutEnabled(user.Id, true));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOut(user.Id));
            UnitTestHelper.IsSuccess(await mgr.AccessFailed(user.Id));
            Assert.False(await mgr.IsLockedOut(user.Id));
            Assert.False(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCount(user.Id));
            UnitTestHelper.IsSuccess(await mgr.AccessFailed(user.Id));
            Assert.True(await mgr.IsLockedOut(user.Id));
            Assert.True(await mgr.GetLockoutEndDate(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCount(user.Id));
        }

        [Fact]
        public async Task UserNotLockedOutWithNullDateTimeAndIsSetToNullDate()
        {
            var mgr = CreateManager();
            mgr.UserLockoutEnabledByDefault = true;
            var user = new InMemoryUser("LockoutTest");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            UnitTestHelper.IsSuccess(await mgr.SetLockoutEndDate(user.Id, new DateTimeOffset()));
            Assert.False(await mgr.IsLockedOut(user.Id));
            Assert.Equal(new DateTimeOffset(), await mgr.GetLockoutEndDate(user.Id));
            Assert.Equal(new DateTimeOffset(), user.LockoutEnd);
        }

        [Fact]
        public async Task LockoutFailsIfNotEnabled()
        {
            var mgr = CreateManager();
            var user = new InMemoryUser("LockoutNotEnabledTest");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.False(await mgr.GetLockoutEnabled(user.Id));
            Assert.False(user.LockoutEnabled);
            UnitTestHelper.IsFailure(await mgr.SetLockoutEndDate(user.Id, new DateTimeOffset()), "Lockout is not enabled for this user.");
            Assert.False(await mgr.IsLockedOut(user.Id));
        }

        [Fact]
        public async Task LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.UserLockoutEnabledByDefault = true;
            var user = new InMemoryUser("LockoutUtcNowTest") { LockoutEnd = DateTimeOffset.UtcNow.AddSeconds(-1) };
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOut(user.Id));
        }

        [Fact]
        public async Task LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.UserLockoutEnabledByDefault = true;
            var user = new InMemoryUser("LockoutUtcNowTest");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            UnitTestHelper.IsSuccess(await mgr.SetLockoutEndDate(user.Id, DateTimeOffset.UtcNow.AddSeconds(-1)));
            Assert.False(await mgr.IsLockedOut(user.Id));
        }

        [Fact]
        public async Task LockoutEndToUtcNowPlus5ShouldBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.UserLockoutEnabledByDefault = true;
            var user = new InMemoryUser("LockoutUtcNowTest") { LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5) };
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            Assert.True(await mgr.IsLockedOut(user.Id));
        }

        [Fact]
        public async Task UserLockedOutWithDateTimeLocalKindNowPlus30()
        {
            var mgr = CreateManager();
            mgr.UserLockoutEnabledByDefault = true;
            var user = new InMemoryUser("LockoutTest");
            UnitTestHelper.IsSuccess(await mgr.Create(user));
            Assert.True(await mgr.GetLockoutEnabled(user.Id));
            Assert.True(user.LockoutEnabled);
            var lockoutEnd = new DateTimeOffset(DateTime.Now.AddMinutes(30).ToLocalTime());
            UnitTestHelper.IsSuccess(await mgr.SetLockoutEndDate(user.Id, lockoutEnd));
            Assert.True(await mgr.IsLockedOut(user.Id));
            var end = await mgr.GetLockoutEndDate(user.Id);
            Assert.Equal(lockoutEnd, end);
        }

        // Role Tests
        [Fact]
        public async Task CreateRoleTest()
        {
            var manager = CreateRoleManager();
            var role = new InMemoryRole("create");
            Assert.False(await manager.RoleExists(role.Name));
            UnitTestHelper.IsSuccess(await manager.Create(role));
            Assert.True(await manager.RoleExists(role.Name));
        }

        //[Fact]
        //public async Task BadValidatorBlocksCreateTest()
        //{
        //    var manager = CreateRoleManager();
        //    manager.RoleValidator = new AlwaysBadValidator<InMemoryRole>();
        //    UnitTestHelper.IsFailure(await manager.Create(new InMemoryRole("blocked")),
        //        AlwaysBadValidator<InMemoryRole>.ErrorMessage);
        //}

        //[Fact]
        //public async Task BadValidatorBlocksAllUpdatesTest()
        //{
        //    var manager = CreateRoleManager();
        //    var role = new InMemoryRole("poorguy");
        //    UnitTestHelper.IsSuccess(await manager.Create(role));
        //    var error = AlwaysBadValidator<InMemoryRole>.ErrorMessage;
        //    manager.RoleValidator = new AlwaysBadValidator<InMemoryRole>();
        //    UnitTestHelper.IsFailure(await manager.Update(role), error);
        //}

        [Fact]
        public async Task DeleteRoleTest()
        {
            var manager = CreateRoleManager();
            var role = new InMemoryRole("delete");
            Assert.False(await manager.RoleExists(role.Name));
            UnitTestHelper.IsSuccess(await manager.Create(role));
            UnitTestHelper.IsSuccess(await manager.Delete(role));
            Assert.False(await manager.RoleExists(role.Name));
        }

        [Fact]
        public async Task RoleFindByIdTest()
        {
            var manager = CreateRoleManager();
            var role = new InMemoryRole("FindById");
            Assert.Null(await manager.FindById(role.Id));
            UnitTestHelper.IsSuccess(await manager.Create(role));
            Assert.Equal(role, await manager.FindById(role.Id));
        }

        [Fact]
        public async Task RoleFindByNameTest()
        {
            var manager = CreateRoleManager();
            var role = new InMemoryRole("FindByName");
            Assert.Null(await manager.FindByName(role.Name));
            Assert.False(await manager.RoleExists(role.Name));
            UnitTestHelper.IsSuccess(await manager.Create(role));
            Assert.Equal(role, await manager.FindByName(role.Name));
        }

        [Fact]
        public async Task UpdateRoleNameTest()
        {
            var manager = CreateRoleManager();
            var role = new InMemoryRole("update");
            Assert.False(await manager.RoleExists(role.Name));
            UnitTestHelper.IsSuccess(await manager.Create(role));
            Assert.True(await manager.RoleExists(role.Name));
            role.Name = "Changed";
            UnitTestHelper.IsSuccess(await manager.Update(role));
            Assert.False(await manager.RoleExists("update"));
            Assert.Equal(role, await manager.FindByName(role.Name));
        }

        [Fact]
        public async Task QuerableRolesTest()
        {
            var manager = CreateRoleManager();
            InMemoryRole[] roles =
            {
                new InMemoryRole("r1"), new InMemoryRole("r2"), new InMemoryRole("r3"),
                new InMemoryRole("r4")
            };
            foreach (var r in roles)
            {
                UnitTestHelper.IsSuccess(await manager.Create(r));
            }
            Assert.Equal(roles.Length, manager.Roles.Count());
            var r1 = manager.Roles.FirstOrDefault(r => r.Name == "r1");
            Assert.Equal(roles[0], r1);
        }

        //[Fact]
        //public async Task DeleteRoleNonEmptySucceedsTest()
        //{
        //    // Need fail if not empty?
        //    var userMgr = CreateManager();
        //    var roleMgr = CreateRoleManager();
        //    var role = new InMemoryRole("deleteNonEmpty");
        //    Assert.False(await roleMgr.RoleExists(role.Name));
        //    UnitTestHelper.IsSuccess(await roleMgr.Create(role));
        //    var user = new InMemoryUser("t");
        //    UnitTestHelper.IsSuccess(await userMgr.Create(user));
        //    UnitTestHelper.IsSuccess(await userMgr.AddToRole(user.Id, role.Name));
        //    UnitTestHelper.IsSuccess(await roleMgr.Delete(role));
        //    Assert.Null(await roleMgr.FindByName(role.Name));
        //    Assert.False(await roleMgr.RoleExists(role.Name));
        //    // REVIEW: We should throw if deleteing a non empty role?
        //    var roles = await userMgr.GetRoles(user.Id);

        //    // In memory this doesn't work since there's no concept of cascading deletes
        //    //Assert.Equal(0, roles.Count());
        //}

        ////[Fact]
        ////public async Task DeleteUserRemovesFromRoleTest()
        ////{
        ////    // Need fail if not empty?
        ////    var userMgr = CreateManager();
        ////    var roleMgr = CreateRoleManager();
        ////    var role = new InMemoryRole("deleteNonEmpty");
        ////    Assert.False(await roleMgr.RoleExists(role.Name));
        ////    UnitTestHelper.IsSuccess(await roleMgr.Create(role));
        ////    var user = new InMemoryUser("t");
        ////    UnitTestHelper.IsSuccess(await userMgr.Create(user));
        ////    UnitTestHelper.IsSuccess(await userMgr.AddToRole(user.Id, role.Name));
        ////    UnitTestHelper.IsSuccess(await userMgr.Delete(user));
        ////    role = roleMgr.FindById(role.Id);
        ////}

        [Fact]
        public async Task CreateRoleFailsIfExistsTest()
        {
            var manager = CreateRoleManager();
            var role = new InMemoryRole("dupeRole");
            Assert.False(await manager.RoleExists(role.Name));
            UnitTestHelper.IsSuccess(await manager.Create(role));
            Assert.True(await manager.RoleExists(role.Name));
            var role2 = new InMemoryRole("dupeRole");
            UnitTestHelper.IsFailure(await manager.Create(role2));
        }

        [Fact]
        public async Task AddUserToRoleTest()
        {
            var manager = CreateManager();
            var roleManager = CreateRoleManager();
            var role = new InMemoryRole("addUserTest");
            UnitTestHelper.IsSuccess(await roleManager.Create(role));
            InMemoryUser[] users =
            {
                new InMemoryUser("1"), new InMemoryUser("2"), new InMemoryUser("3"),
                new InMemoryUser("4")
            };
            foreach (var u in users)
            {
                UnitTestHelper.IsSuccess(await manager.Create(u));
                UnitTestHelper.IsSuccess(await manager.AddToRole(u.Id, role.Name));
                Assert.True(await manager.IsInRole(u.Id, role.Name));
            }
        }

        [Fact]
        public async Task GetRolesForUserTest()
        {
            var userManager = CreateManager();
            var roleManager = CreateRoleManager();
            InMemoryUser[] users =
            {
                new InMemoryUser("u1"), new InMemoryUser("u2"), new InMemoryUser("u3"),
                new InMemoryUser("u4")
            };
            InMemoryRole[] roles =
            {
                new InMemoryRole("r1"), new InMemoryRole("r2"), new InMemoryRole("r3"),
                new InMemoryRole("r4")
            };
            foreach (var u in users)
            {
                UnitTestHelper.IsSuccess(await userManager.Create(u));
            }
            foreach (var r in roles)
            {
                UnitTestHelper.IsSuccess(await roleManager.Create(r));
                foreach (var u in users)
                {
                    UnitTestHelper.IsSuccess(await userManager.AddToRole(u.Id, r.Name));
                    Assert.True(await userManager.IsInRole(u.Id, r.Name));
                }
            }

            foreach (var u in users)
            {
                var rs = await userManager.GetRoles(u.Id);
                Assert.Equal(roles.Length, rs.Count);
                foreach (var r in roles)
                {
                    Assert.True(rs.Any(role => role == r.Name));
                }
            }
        }


        [Fact]
        public async Task RemoveUserFromRoleWithMultipleRoles()
        {
            var userManager = CreateManager();
            var roleManager = CreateRoleManager();
            var user = new InMemoryUser("MultiRoleUser");
            UnitTestHelper.IsSuccess(await userManager.Create(user));
            InMemoryRole[] roles =
            {
                new InMemoryRole("r1"), new InMemoryRole("r2"), new InMemoryRole("r3"),
                new InMemoryRole("r4")
            };
            foreach (var r in roles)
            {
                UnitTestHelper.IsSuccess(await roleManager.Create(r));
                UnitTestHelper.IsSuccess(await userManager.AddToRole(user.Id, r.Name));
                Assert.True(await userManager.IsInRole(user.Id, r.Name));
            }
            UnitTestHelper.IsSuccess(await userManager.RemoveFromRole(user.Id, roles[2].Name));
            Assert.False(await userManager.IsInRole(user.Id, roles[2].Name));
        }

        [Fact]
        public async Task RemoveUserFromRoleTest()
        {
            var userManager = CreateManager();
            var roleManager = CreateRoleManager();
            InMemoryUser[] users =
            {
                new InMemoryUser("1"), new InMemoryUser("2"), new InMemoryUser("3"),
                new InMemoryUser("4")
            };
            foreach (var u in users)
            {
                UnitTestHelper.IsSuccess(await userManager.Create(u));
            }
            var r = new InMemoryRole("r1");
            UnitTestHelper.IsSuccess(await roleManager.Create(r));
            foreach (var u in users)
            {
                UnitTestHelper.IsSuccess(await userManager.AddToRole(u.Id, r.Name));
                Assert.True(await userManager.IsInRole(u.Id, r.Name));
            }
            foreach (var u in users)
            {
                UnitTestHelper.IsSuccess(await userManager.RemoveFromRole(u.Id, r.Name));
                Assert.False(await userManager.IsInRole(u.Id, r.Name));
            }
        }

        [Fact]
        public async Task RemoveUserNotInRoleFailsTest()
        {
            var userMgr = CreateManager();
            var roleMgr = CreateRoleManager();
            var role = new InMemoryRole("addUserDupeTest");
            var user = new InMemoryUser("user1");
            UnitTestHelper.IsSuccess(await userMgr.Create(user));
            UnitTestHelper.IsSuccess(await roleMgr.Create(role));
            var result = await userMgr.RemoveFromRole(user.Id, role.Name);
            UnitTestHelper.IsFailure(result, "User is not in role.");
        }

        [Fact]
        public async Task AddUserToRoleFailsIfAlreadyInRoleTest()
        {
            var userMgr = CreateManager();
            var roleMgr = CreateRoleManager();
            var role = new InMemoryRole("addUserDupeTest");
            var user = new InMemoryUser("user1");
            UnitTestHelper.IsSuccess(await userMgr.Create(user));
            UnitTestHelper.IsSuccess(await roleMgr.Create(role));
            UnitTestHelper.IsSuccess(await userMgr.AddToRole(user.Id, role.Name));
            Assert.True(await userMgr.IsInRole(user.Id, role.Name));
            UnitTestHelper.IsFailure(await userMgr.AddToRole(user.Id, role.Name), "User already in role.");
        }

        [Fact]
        public async Task FindRoleByNameWithManagerTest()
        {
            var roleMgr = CreateRoleManager();
            var role = new InMemoryRole("findRoleByNameTest");
            UnitTestHelper.IsSuccess(await roleMgr.Create(role));
            Assert.Equal(role.Id, (await roleMgr.FindByName(role.Name)).Id);
        }

        [Fact]
        public async Task FindRoleWithManagerTest()
        {
            var roleMgr = CreateRoleManager();
            var role = new InMemoryRole("findRoleTest");
            UnitTestHelper.IsSuccess(await roleMgr.Create(role));
            Assert.Equal(role.Name, (await roleMgr.FindById(role.Id)).Name);
        }


        private static UserManager<InMemoryUser, string> CreateManager()
        {
            return new UserManager<InMemoryUser, string>(new InMemoryUserStore<InMemoryUser>());
        }

        private static RoleManager<InMemoryRole> CreateRoleManager()
        {
            return new RoleManager<InMemoryRole>(new InMemoryRoleStore());
        }
    }
}