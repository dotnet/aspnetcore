//using Microsoft.AspNet.DependencyInjection.Fallback;
//using Microsoft.AspNet.Testing;
//using Microsoft.Data.Entity;
//using Microsoft.Data.Entity.Metadata;
//using Microsoft.Data.Entity.Storage;
//using Microsoft.Data.InMemory;
//using System;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading;
//using System.Threading.Tasks;
//using Xunit;

//namespace Microsoft.AspNet.Identity.Entity.Test
//{
//    public class UserStoreTest
//    {
//        [Fact]
//        public async Task Can_share_instance_between_contexts_with_sugar_experience2()
//        {
//            using (var db = new IdentityContext())
//            {
//                db.Users.Add(new IdentityUser { UserName = "John Doe" });
//                await db.SaveChangesAsync();
//            }

//            using (var db = new IdentityContext())
//            {
//                var data = db.Users.ToList();
//                Assert.Equal(1, data.Count);
//                Assert.Equal("John Doe", data[0].UserName);
//            }
//        }

//        [Fact]
//        public async Task Can_share_instance_between_contexts_with_sugar_experience()
//        {
//            using (var db = new SimpleContext())
//            {
//                db.Artists.Add(new SimpleContext.Artist { Name = "John Doe" });
//                await db.SaveChangesAsync();
//            }

//            using (var db = new SimpleContext())
//            {
//                var data = db.Artists.ToList();
//                Assert.Equal(1, data.Count);
//                Assert.Equal("John Doe", data[0].Name);
//            }
//        }

//        [Fact]
//        public async Task Can_create_two_artists()
//        {
//            using (var db = new SimpleContext())
//            {
//                db.Artists.Add(new SimpleContext.Artist { Name = "John Doe", ArtistId = Guid.NewGuid().ToString() });
//                await db.SaveChangesAsync();
//                db.Artists.Add(new SimpleContext.Artist { Name = "Second guy", ArtistId = Guid.NewGuid().ToString() });
//                await db.SaveChangesAsync();
//            }
//        }

//        private class SimpleContext : EntityContext
//        {
//            public EntitySet<Artist> Artists { get; set; }

//            protected override void OnConfiguring(EntityConfigurationBuilder builder)
//            {
//                builder.UseDataStore(new InMemoryDataStore());
//            }

//            protected override void OnModelCreating(ModelBuilder builder)
//            {
//                builder.Entity<Artist>().Key(a => a.ArtistId);
//            }

//            public class Artist// : ArtistBase<string>
//            {
//                public string ArtistId { get; set; }
//                public string Name { get; set; }
//            }

//            public class ArtistBase<TKey>
//            {
//                public TKey ArtistId { get; set; }
//                public string Name { get; set; }
//            }
//        }

//        [Fact]
//        public async Task Foo()
//        {
//            using (var db = new IdentityContext())
//            {
//                db.Users.Add(new IdentityUser("A"));
//                await db.SaveChangesAsync();
//            }

//            using (var db = new IdentityContext())
//            {
//                var data = db.Users.ToList();
//                Assert.Equal(1, data.Count);
//                Assert.Equal("A", data[0].UserName);
//            }
//        }

//        [Fact]
//        public async Task CanDeleteUser()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("DeleteAsync");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
//            Assert.Null(await manager.FindByIdAsync(user.Id));
//        }

//        //[Fact]
//        //public async Task CanUpdateUserName()
//        //{
//        //    var manager = CreateManager();
//        //    var user = new IdentityUser("UpdateAsync");
//        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//        //    Assert.Null(await manager.FindByNameAsync("New"));
//        //    user.UserName = "New";
//        //    IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
//        //    Assert.NotNull(await manager.FindByNameAsync("New"));
//        //    Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
//        //}

//        [Fact]
//        public async Task UserValidatorCanBlockCreate()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("CreateBlocked");
//            manager.UserValidator = new AlwaysBadValidator();
//            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
//        }

//        //[Fact]
//        //public async Task UserValidatorCanBlockUpdate()
//        //{
//        //    var manager = CreateManager();
//        //    var user = new IdentityUser("UpdateBlocked");
//        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//        //    manager.UserValidator = new AlwaysBadValidator();
//        //    IdentityResultAssert.IsFailure(await manager.UpdateAsync(user), AlwaysBadValidator.ErrorMessage);
//        //}

//        //        [Theory]
//        //        [InlineData("")]
//        //        [InlineData(null)]
//        //        public async Task UserValidatorBlocksShortEmailsWhenRequiresUniqueEmail(string email)
//        //        {
//        //            var manager = CreateManager();
//        //            var user = new IdentityUser("UpdateBlocked") {Email = email};
//        //            manager.UserValidator = new UserValidator<IdentityUser, string> {RequireUniqueEmail = true};
//        //            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email cannot be null or empty.");
//        //        }

//        //#if NET45
//        //        [Theory]
//        //        [InlineData("@@afd")]
//        //        [InlineData("bogus")]
//        //        public async Task UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail(string email)
//        //        {
//        //            var manager = CreateManager();
//        //            var user = new IdentityUser("UpdateBlocked") {Email = email};
//        //            manager.UserValidator = new UserValidator<IdentityUser, string> {RequireUniqueEmail = true};
//        //            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email '" + email + "' is invalid.");
//        //        }
//        //#endif

//        //        [Fact]
//        //        public async Task PasswordValidatorCanBlockAddPassword()
//        //        {
//        //            var manager = CreateManager();
//        //            var user = new IdentityUser("AddPasswordBlocked");
//        //            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//        //            manager.PasswordValidator = new AlwaysBadValidator();
//        //            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user.Id, "password"),
//        //                AlwaysBadValidator.ErrorMessage);
//        //        }

//        [Fact]
//        public async Task PasswordValidatorCanBlockChangePassword()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("ChangePasswordBlocked");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
//            manager.PasswordValidator = new AlwaysBadValidator();
//            IdentityResultAssert.IsFailure(await manager.ChangePasswordAsync(user.Id, "password", "new"),
//                AlwaysBadValidator.ErrorMessage);
//        }

//        [Fact]
//        public async Task CanCreateUserNoPassword()
//        {
//            var manager = CreateManager();
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityUser("CreateUserTest")));
//            var user = await manager.FindByNameAsync("CreateUserTest");
//            Assert.NotNull(user);
//            Assert.Null(user.PasswordHash);
//            var logins = await manager.GetLoginsAsync(user.Id);
//            Assert.NotNull(logins);
//            Assert.Equal(0, logins.Count());
//        }

//        [Fact]
//        public async Task CanCreateUserAddLogin()
//        {
//            var manager = CreateManager();
//            const string userName = "CreateExternalUserTest";
//            const string provider = "ZzAuth";
//            const string providerKey = "HaoKey";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityUser(userName)));
//            var user = await manager.FindByNameAsync(userName);
//            Assert.NotNull(user);
//            var login = new UserLoginInfo(provider, providerKey);
//            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user.Id, login));
//            var logins = await manager.GetLoginsAsync(user.Id);
//            Assert.NotNull(logins);
//            Assert.Equal(1, logins.Count());
//            Assert.Equal(provider, logins.First().LoginProvider);
//            Assert.Equal(providerKey, logins.First().ProviderKey);
//        }

//        [Fact]
//        public async Task CanCreateUserLoginAndAddPassword()
//        {
//            var manager = CreateManager();
//            var login = new UserLoginInfo("Provider", "key");
//            var user = new IdentityUser("CreateUserLoginAddPasswordTest");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user.Id, login));
//            Assert.False(await manager.HasPasswordAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await manager.AddPasswordAsync(user.Id, "password"));
//            Assert.True(await manager.HasPasswordAsync(user.Id));
//            var logins = await manager.GetLoginsAsync(user.Id);
//            Assert.NotNull(logins);
//            Assert.Equal(1, logins.Count());
//            Assert.Equal(user, await manager.FindByLoginAsync(login));
//            Assert.Equal(user, await manager.FindByLoginAsync(user.UserName, "password"));
//        }

//        [Fact]
//        public async Task AddPasswordFailsIfAlreadyHave()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("CannotAddAnotherPassword");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "Password"));
//            Assert.True(await manager.HasPasswordAsync(user.Id));
//            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user.Id, "password"),
//                "User already has a password set.");
//        }

//        [Fact]
//        public async Task CanCreateUserAddRemoveLogin()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("CreateUserAddRemoveLoginTest");
//            var login = new UserLoginInfo("Provider", "key");
//            var result = await manager.CreateAsync(user);
//            Assert.NotNull(user);
//            IdentityResultAssert.IsSuccess(result);
//            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user.Id, login));
//            Assert.Equal(user, await manager.FindByLoginAsync(login));
//            var logins = await manager.GetLoginsAsync(user.Id);
//            Assert.NotNull(logins);
//            Assert.Equal(1, logins.Count());
//            Assert.Equal(login.LoginProvider, logins.Last().LoginProvider);
//            Assert.Equal(login.ProviderKey, logins.Last().ProviderKey);
//            var stamp = user.SecurityStamp;
//            IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user.Id, login));
//            Assert.Null(await manager.FindByLoginAsync(login));
//            logins = await manager.GetLoginsAsync(user.Id);
//            Assert.NotNull(logins);
//            Assert.Equal(0, logins.Count());
//            Assert.NotEqual(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task CanRemovePassword()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("RemovePasswordTest");
//            const string password = "password";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
//            var stamp = user.SecurityStamp;
//            IdentityResultAssert.IsSuccess(await manager.RemovePasswordAsync(user.Id));
//            var u = await manager.FindByNameAsync(user.UserName);
//            Assert.NotNull(u);
//            Assert.Null(u.PasswordHash);
//            Assert.NotEqual(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task CanChangePassword()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("ChangePasswordTest");
//            const string password = "password";
//            const string newPassword = "newpassword";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
//            Assert.Equal(manager.Users.Count(), 1);
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user.Id, password, newPassword));
//            Assert.Null(await manager.FindByLoginAsync(user.UserName, password));
//            Assert.Equal(user, await manager.FindByLoginAsync(user.UserName, newPassword));
//            Assert.NotEqual(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task CanAddRemoveUserClaim()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("ClaimsAddRemove");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
//            foreach (var c in claims)
//            {
//                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user.Id, c));
//            }
//            var userClaims = await manager.GetClaimsAsync(user.Id);
//            Assert.Equal(3, userClaims.Count);
//            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user.Id, claims[0]));
//            userClaims = await manager.GetClaimsAsync(user.Id);
//            Assert.Equal(2, userClaims.Count);
//            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user.Id, claims[1]));
//            userClaims = await manager.GetClaimsAsync(user.Id);
//            Assert.Equal(1, userClaims.Count);
//            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user.Id, claims[2]));
//            userClaims = await manager.GetClaimsAsync(user.Id);
//            Assert.Equal(0, userClaims.Count);
//        }

//        [Fact]
//        public async Task ChangePasswordFallsIfPasswordWrong()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("user");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
//            var result = await manager.ChangePasswordAsync(user.Id, "bogus", "newpassword");
//            IdentityResultAssert.IsFailure(result, "Incorrect password.");
//        }

//        [Fact]
//        public async Task AddDupeUserNameFails()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("dupe");
//            var user2 = new IdentityUser("dupe");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Name dupe is already taken.");
//        }

//        [Fact]
//        public async Task AddDupeEmailAllowedByDefault()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("dupe") { Email = "yup@yup.com" };
//            var user2 = new IdentityUser("dupeEmail") { Email = "yup@yup.com" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
//        }

//        [Fact]
//        public async Task AddDupeEmailFallsWhenUniqueEmailRequired()
//        {
//            var manager = CreateManager();
//            manager.UserValidator = new UserValidator<IdentityUser> { RequireUniqueEmail = true };
//            var user = new IdentityUser("dupe") { Email = "yup@yup.com" };
//            var user2 = new IdentityUser("dupeEmail") { Email = "yup@yup.com" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Email 'yup@yup.com' is already taken.");
//        }

//        [Fact]
//        public async Task UpdateSecurityStampActuallyChanges()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("stampMe");
//            Assert.Null(user.SecurityStamp);
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user.Id));
//            Assert.NotEqual(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task AddDupeLoginFails()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("DupeLogin");
//            var login = new UserLoginInfo("provder", "key");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user.Id, login));
//            var result = await manager.AddLoginAsync(user.Id, login);
//            IdentityResultAssert.IsFailure(result, "A user with that external login already exists.");
//        }

//        // Email tests
//        [Fact]
//        public async Task CanFindByEmail()
//        {
//            var manager = CreateManager();
//            const string userName = "EmailTest";
//            const string email = "email@test.com";
//            var user = new IdentityUser(userName) { Email = email };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var fetch = await manager.FindByEmailAsync(email);
//            Assert.Equal(user, fetch);
//        }

//        [Fact]
//        public async Task CanFindUsersViaUserQuerable()
//        {
//            var mgr = CreateManager();
//            var users = new[]
//            {
//                new IdentityUser("user1"),
//                new IdentityUser("user2"),
//                new IdentityUser("user3")
//            };
//            foreach (var u in users)
//            {
//                IdentityResultAssert.IsSuccess(await mgr.CreateAsync(u));
//            }
//            var usersQ = mgr.Users;
//            Assert.Equal(3, usersQ.Count());
//            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user1"));
//            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user2"));
//            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user3"));
//            Assert.Null(usersQ.FirstOrDefault(u => u.UserName == "bogus"));
//        }

//        [Fact]
//        public async Task ClaimsIdentityCreatesExpectedClaims()
//        {
//            var context = CreateContext();
//            var manager = CreateManager(context);
//            var role = CreateRoleManager(context);
//            var user = new IdentityUser("Hao");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await role.CreateAsync(new IdentityRole("Admin")));
//            IdentityResultAssert.IsSuccess(await role.CreateAsync(new IdentityRole("Local")));
//            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user.Id, "Admin"));
//            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user.Id, "Local"));
//            Claim[] userClaims =
//            {
//                new Claim("Whatever", "Value"),
//                new Claim("Whatever2", "Value2")
//            };
//            foreach (var c in userClaims)
//            {
//                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user.Id, c));
//            }

//            var identity = await manager.CreateIdentity(user, "test");
//            var claimsFactory = manager.ClaimsIdentityFactory as ClaimsIdentityFactory<IdentityUser>;
//            Assert.NotNull(claimsFactory);
//            var claims = identity.Claims;
//            Assert.NotNull(claims);
//            Assert.True(
//                claims.Any(c => c.Type == claimsFactory.UserNameClaimType && c.Value == user.UserName));
//            Assert.True(claims.Any(c => c.Type == claimsFactory.UserIdClaimType && c.Value == user.Id));
//            Assert.True(claims.Any(c => c.Type == claimsFactory.RoleClaimType && c.Value == "Admin"));
//            Assert.True(claims.Any(c => c.Type == claimsFactory.RoleClaimType && c.Value == "Local"));
//            foreach (var cl in userClaims)
//            {
//                Assert.True(claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
//            }
//        }

//        [Fact]
//        public async Task ConfirmEmailFalseByDefaultTest()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("test");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            Assert.False(await manager.IsEmailConfirmedAsync(user.Id));
//        }

//        // TODO: No token provider implementations yet
//        private class StaticTokenProvider : IUserTokenProvider<IdentityUser>
//        {
//            public Task<string> GenerateAsync(string purpose, UserManager<IdentityUser> manager,
//                IdentityUser user, CancellationToken token)
//            {
//                return Task.FromResult(MakeToken(purpose, user));
//            }

//            public Task<bool> ValidateAsync(string purpose, string token, UserManager<IdentityUser> manager,
//                IdentityUser user, CancellationToken cancellationToken)
//            {
//                return Task.FromResult(token == MakeToken(purpose, user));
//            }

//            public Task NotifyAsync(string token, UserManager<IdentityUser> manager, IdentityUser user, CancellationToken cancellationToken)
//            {
//                return Task.FromResult(0);
//            }

//            public Task<bool> IsValidProviderForUserAsync(UserManager<IdentityUser> manager, IdentityUser user, CancellationToken token)
//            {
//                return Task.FromResult(true);
//            }

//            private static string MakeToken(string purpose, IdentityUser user)
//            {
//                return string.Join(":", user.Id, purpose, "ImmaToken");
//            }
//        }

//        [Fact]
//        public async Task CanResetPasswordWithStaticTokenProvider()
//        {
//            var manager = CreateManager();
//            manager.UserTokenProvider = new StaticTokenProvider();
//            var user = new IdentityUser("ResetPasswordTest");
//            const string password = "password";
//            const string newPassword = "newpassword";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            var token = await manager.GeneratePasswordResetTokenAsync(user.Id);
//            Assert.NotNull(token);
//            IdentityResultAssert.IsSuccess(await manager.ResetPassword(user.Id, token, newPassword));
//            Assert.Null(await manager.FindByLoginAsync(user.UserName, password));
//            Assert.Equal(user, await manager.FindByLoginAsync(user.UserName, newPassword));
//            Assert.NotEqual(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task PasswordValidatorCanBlockResetPasswordWithStaticTokenProvider()
//        {
//            var manager = CreateManager();
//            manager.UserTokenProvider = new StaticTokenProvider();
//            var user = new IdentityUser("ResetPasswordTest");
//            const string password = "password";
//            const string newPassword = "newpassword";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            var token = await manager.GeneratePasswordResetTokenAsync(user.Id);
//            Assert.NotNull(token);
//            manager.PasswordValidator = new AlwaysBadValidator();
//            IdentityResultAssert.IsFailure(await manager.ResetPassword(user.Id, token, newPassword),
//                AlwaysBadValidator.ErrorMessage);
//            Assert.NotNull(await manager.FindByLoginAsync(user.UserName, password));
//            Assert.Equal(user, await manager.FindByLoginAsync(user.UserName, password));
//            Assert.Equal(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task ResetPasswordWithStaticTokenProviderFailsWithWrongToken()
//        {
//            var manager = CreateManager();
//            manager.UserTokenProvider = new StaticTokenProvider();
//            var user = new IdentityUser("ResetPasswordTest");
//            const string password = "password";
//            const string newPassword = "newpassword";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            IdentityResultAssert.IsFailure(await manager.ResetPassword(user.Id, "bogus", newPassword), "Invalid token.");
//            Assert.NotNull(await manager.FindByLoginAsync(user.UserName, password));
//            Assert.Equal(user, await manager.FindByLoginAsync(user.UserName, password));
//            Assert.Equal(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task CanGenerateAndVerifyUserTokenWithStaticTokenProvider()
//        {
//            var manager = CreateManager();
//            manager.UserTokenProvider = new StaticTokenProvider();
//            var user = new IdentityUser("UserTokenTest");
//            var user2 = new IdentityUser("UserTokenTest2");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
//            var token = await manager.GenerateUserTokenAsync("test", user.Id);
//            Assert.True(await manager.VerifyUserTokenAsync(user.Id, "test", token));
//            Assert.False(await manager.VerifyUserTokenAsync(user.Id, "test2", token));
//            Assert.False(await manager.VerifyUserTokenAsync(user.Id, "test", token + "a"));
//            Assert.False(await manager.VerifyUserTokenAsync(user2.Id, "test", token));
//        }

//        [Fact]
//        public async Task CanConfirmEmailWithStaticToken()
//        {
//            var manager = CreateManager();
//            manager.UserTokenProvider = new StaticTokenProvider();
//            var user = new IdentityUser("test");
//            Assert.False(user.EmailConfirmed);
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var token = await manager.GenerateEmailConfirmationTokenAsync(user.Id);
//            Assert.NotNull(token);
//            IdentityResultAssert.IsSuccess(await manager.ConfirmEmailAsync(user.Id, token));
//            Assert.True(await manager.IsEmailConfirmedAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user.Id, null));
//            Assert.False(await manager.IsEmailConfirmedAsync(user.Id));
//        }

//        [Fact]
//        public async Task ConfirmEmailWithStaticTokenFailsWithWrongToken()
//        {
//            var manager = CreateManager();
//            manager.UserTokenProvider = new StaticTokenProvider();
//            var user = new IdentityUser("test");
//            Assert.False(user.EmailConfirmed);
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user.Id, "bogus"), "Invalid token.");
//            Assert.False(await manager.IsEmailConfirmedAsync(user.Id));
//        }

//        // TODO: Can't reenable til we have a SecurityStamp linked token provider
//        //[Fact]
//        //public async Task ConfirmTokenFailsAfterPasswordChange()
//        //{
//        //    var manager = CreateManager();
//        //    var user = new IdentityUser("test");
//        //    Assert.False(user.EmailConfirmed);
//        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
//        //    var token = await manager.GenerateEmailConfirmationTokenAsync(user.Id);
//        //    Assert.NotNull(token);
//        //    IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user.Id, "password", "newpassword"));
//        //    IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user.Id, token), "Invalid token.");
//        //    Assert.False(await manager.IsEmailConfirmedAsync(user.Id));
//        //}

//        // Lockout tests

//        [Fact]
//        public async Task SingleFailureLockout()
//        {
//            var mgr = CreateManager();
//            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
//            mgr.UserLockoutEnabledByDefault = true;
//            var user = new IdentityUser("fastLockout");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user.Id));
//            Assert.True(await mgr.IsLockedOutAsync(user.Id));
//            Assert.True(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user.Id));
//        }

//        [Fact]
//        public async Task TwoFailureLockout()
//        {
//            var mgr = CreateManager();
//            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
//            mgr.UserLockoutEnabledByDefault = true;
//            mgr.MaxFailedAccessAttemptsBeforeLockout = 2;
//            var user = new IdentityUser("twoFailureLockout");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user.Id));
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            Assert.False(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user.Id));
//            Assert.True(await mgr.IsLockedOutAsync(user.Id));
//            Assert.True(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user.Id));
//        }

//        [Fact]
//        public async Task ResetAccessCountPreventsLockout()
//        {
//            var mgr = CreateManager();
//            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
//            mgr.UserLockoutEnabledByDefault = true;
//            mgr.MaxFailedAccessAttemptsBeforeLockout = 2;
//            var user = new IdentityUser("resetLockout");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user.Id));
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            Assert.False(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await mgr.ResetAccessFailedCountAsync(user.Id));
//            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user.Id));
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            Assert.False(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user.Id));
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            Assert.False(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user.Id));
//        }

//        [Fact]
//        public async Task CanEnableLockoutManuallyAndLockout()
//        {
//            var mgr = CreateManager();
//            mgr.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(1);
//            mgr.MaxFailedAccessAttemptsBeforeLockout = 2;
//            var user = new IdentityUser("manualLockout");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.False(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.False(user.LockoutEnabled);
//            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEnabledAsync(user.Id, true));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user.Id));
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            Assert.False(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user.Id));
//            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user.Id));
//            Assert.True(await mgr.IsLockedOutAsync(user.Id));
//            Assert.True(await mgr.GetLockoutEndDateAsync(user.Id) > DateTimeOffset.UtcNow.AddMinutes(55));
//            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user.Id));
//        }

//        [Fact]
//        public async Task UserNotLockedOutWithNullDateTimeAndIsSetToNullDate()
//        {
//            var mgr = CreateManager();
//            mgr.UserLockoutEnabledByDefault = true;
//            var user = new IdentityUser("LockoutTest");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user.Id, new DateTimeOffset()));
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//            Assert.Equal(new DateTimeOffset(), await mgr.GetLockoutEndDateAsync(user.Id));
//            Assert.Null(user.LockoutEndDateUtc);
//        }

//        [Fact]
//        public async Task LockoutFailsIfNotEnabled()
//        {
//            var mgr = CreateManager();
//            var user = new IdentityUser("LockoutNotEnabledTest");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.False(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.False(user.LockoutEnabled);
//            IdentityResultAssert.IsFailure(await mgr.SetLockoutEndDateAsync(user.Id, new DateTimeOffset()),
//                "Lockout is not enabled for this user.");
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//        }

//        [Fact]
//        public async Task LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut()
//        {
//            var mgr = CreateManager();
//            mgr.UserLockoutEnabledByDefault = true;
//            var user = new IdentityUser("LockoutUtcNowTest") { LockoutEndDateUtc = DateTime.UtcNow.AddSeconds(-1) };
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//        }

//        [Fact]
//        public async Task LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut()
//        {
//            var mgr = CreateManager();
//            mgr.UserLockoutEnabledByDefault = true;
//            var user = new IdentityUser("LockoutUtcNowTest");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user.Id, DateTimeOffset.UtcNow.AddSeconds(-1)));
//            Assert.False(await mgr.IsLockedOutAsync(user.Id));
//        }

//        [Fact]
//        public async Task LockoutEndToUtcNowPlus5ShouldBeLockedOut()
//        {
//            var mgr = CreateManager();
//            mgr.UserLockoutEnabledByDefault = true;
//            var user = new IdentityUser("LockoutUtcNowTest") { LockoutEndDateUtc = DateTime.UtcNow.AddMinutes(5) };
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            Assert.True(await mgr.IsLockedOutAsync(user.Id));
//        }

//        [Fact]
//        public async Task UserLockedOutWithDateTimeLocalKindNowPlus30()
//        {
//            var mgr = CreateManager();
//            mgr.UserLockoutEnabledByDefault = true;
//            var user = new IdentityUser("LockoutTest");
//            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
//            Assert.True(await mgr.GetLockoutEnabledAsync(user.Id));
//            Assert.True(user.LockoutEnabled);
//            var lockoutEnd = new DateTimeOffset(DateTime.Now.AddMinutes(30).ToLocalTime());
//            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user.Id, lockoutEnd));
//            Assert.True(await mgr.IsLockedOutAsync(user.Id));
//            var end = await mgr.GetLockoutEndDateAsync(user.Id);
//            Assert.Equal(lockoutEnd, end);
//        }

//        // Role Tests
//        [Fact]
//        public async Task CanCreateRoleTest()
//        {
//            var manager = CreateRoleManager();
//            var role = new IdentityRole("create");
//            Assert.False(await manager.RoleExistsAsync(role.Name));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
//            Assert.True(await manager.RoleExistsAsync(role.Name));
//        }

//        private class AlwaysBadValidator : IUserValidator<IdentityUser>, IRoleValidator<IdentityRole>,
//            IPasswordValidator
//        {
//            public const string ErrorMessage = "I'm Bad.";

//            public Task<IdentityResult> ValidateAsync(string password, CancellationToken token)
//            {
//                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
//            }

//            public Task<IdentityResult> ValidateAsync(RoleManager<IdentityRole> manager, IdentityRole role, CancellationToken token)
//            {
//                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
//            }

//            public Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, CancellationToken token)
//            {
//                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
//            }
//        }

//        [Fact]
//        public async Task BadValidatorBlocksCreateRole()
//        {
//            var manager = CreateRoleManager();
//            manager.RoleValidator = new AlwaysBadValidator();
//            IdentityResultAssert.IsFailure(await manager.CreateAsync(new IdentityRole("blocked")),
//                AlwaysBadValidator.ErrorMessage);
//        }

//        [Fact]
//        public async Task BadValidatorBlocksRoleUpdate()
//        {
//            var manager = CreateRoleManager();
//            var role = new IdentityRole("poorguy");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
//            var error = AlwaysBadValidator.ErrorMessage;
//            manager.RoleValidator = new AlwaysBadValidator();
//            IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
//        }

//        [Fact]
//        public async Task CanDeleteRoleTest()
//        {
//            var manager = CreateRoleManager();
//            var role = new IdentityRole("delete");
//            Assert.False(await manager.RoleExistsAsync(role.Name));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
//            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
//            Assert.False(await manager.RoleExistsAsync(role.Name));
//        }

//        [Fact]
//        public async Task CanRoleFindByIdTest()
//        {
//            var manager = CreateRoleManager();
//            var role = new IdentityRole("FindByIdAsync");
//            Assert.Null(await manager.FindByIdAsync(role.Id));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
//            Assert.Equal(role, await manager.FindByIdAsync(role.Id));
//        }

//        [Fact]
//        public async Task CanRoleFindByName()
//        {
//            var manager = CreateRoleManager();
//            var role = new IdentityRole("FindByNameAsync");
//            Assert.Null(await manager.FindByNameAsync(role.Name));
//            Assert.False(await manager.RoleExistsAsync(role.Name));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
//            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
//        }

//        [Fact]
//        public async Task CanUpdateRoleNameTest()
//        {
//            var manager = CreateRoleManager();
//            var role = new IdentityRole("update");
//            Assert.False(await manager.RoleExistsAsync(role.Name));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
//            Assert.True(await manager.RoleExistsAsync(role.Name));
//            role.Name = "Changed";
//            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
//            Assert.False(await manager.RoleExistsAsync("update"));
//            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
//        }

//        [Fact]
//        public async Task CanQuerableRolesTest()
//        {
//            var manager = CreateRoleManager();
//            IdentityRole[] roles =
//            {
//                new IdentityRole("r1"), new IdentityRole("r2"), new IdentityRole("r3"),
//                new IdentityRole("r4")
//            };
//            foreach (var r in roles)
//            {
//                IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
//            }
//            Assert.Equal(roles.Length, manager.Roles.Count());
//            var r1 = manager.Roles.FirstOrDefault(r => r.Name == "r1");
//            Assert.Equal(roles[0], r1);
//        }

//        //[Fact]
//        //public async Task DeleteRoleNonEmptySucceedsTest()
//        //{
//        //    // Need fail if not empty?
//        //    var userMgr = CreateManager();
//        //    var roleMgr = CreateRoleManager();
//        //    var role = new IdentityRole("deleteNonEmpty");
//        //    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
//        //    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
//        //    var user = new IdentityUser("t");
//        //    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
//        //    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user.Id, role.Name));
//        //    IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
//        //    Assert.Null(await roleMgr.FindByNameAsync(role.Name));
//        //    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
//        //    // REVIEW: We should throw if deleteing a non empty role?
//        //    var roles = await userMgr.GetRolesAsync(user.Id);

//        //    // In memory this doesn't work since there's no concept of cascading deletes
//        //    //Assert.Equal(0, roles.Count());
//        //}

//        ////[Fact]
//        ////public async Task DeleteUserRemovesFromRoleTest()
//        ////{
//        ////    // Need fail if not empty?
//        ////    var userMgr = CreateManager();
//        ////    var roleMgr = CreateRoleManager();
//        ////    var role = new IdentityRole("deleteNonEmpty");
//        ////    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
//        ////    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
//        ////    var user = new IdentityUser("t");
//        ////    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
//        ////    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user.Id, role.Name));
//        ////    IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));
//        ////    role = roleMgr.FindByIdAsync(role.Id);
//        ////}

//        [Fact]
//        public async Task CreateRoleFailsIfExists()
//        {
//            var manager = CreateRoleManager();
//            var role = new IdentityRole("dupeRole");
//            Assert.False(await manager.RoleExistsAsync(role.Name));
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
//            Assert.True(await manager.RoleExistsAsync(role.Name));
//            var role2 = new IdentityRole("dupeRole");
//            IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
//        }

//        [Fact]
//        public async Task CanAddUsersToRole()
//        {
//            var context = CreateContext();
//            var manager = CreateManager(context);
//            var roleManager = CreateRoleManager(context);
//            var role = new IdentityRole("addUserTest");
//            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
//            IdentityUser[] users =
//            {
//                new IdentityUser("1"), new IdentityUser("2"), new IdentityUser("3"),
//                new IdentityUser("4")
//            };
//            foreach (var u in users)
//            {
//                IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
//                IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(u.Id, role.Name));
//                Assert.True(await manager.IsInRoleAsync(u.Id, role.Name));
//            }
//        }

//        [Fact]
//        public async Task CanGetRolesForUser()
//        {
//            var context = CreateContext();
//            var userManager = CreateManager(context);
//            var roleManager = CreateRoleManager(context);
//            IdentityUser[] users =
//            {
//                new IdentityUser("u1"), new IdentityUser("u2"), new IdentityUser("u3"),
//                new IdentityUser("u4")
//            };
//            IdentityRole[] roles =
//            {
//                new IdentityRole("r1"), new IdentityRole("r2"), new IdentityRole("r3"),
//                new IdentityRole("r4")
//            };
//            foreach (var u in users)
//            {
//                IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
//            }
//            foreach (var r in roles)
//            {
//                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
//                foreach (var u in users)
//                {
//                    IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u.Id, r.Name));
//                    Assert.True(await userManager.IsInRoleAsync(u.Id, r.Name));
//                }
//            }

//            foreach (var u in users)
//            {
//                var rs = await userManager.GetRolesAsync(u.Id);
//                Assert.Equal(roles.Length, rs.Count);
//                foreach (var r in roles)
//                {
//                    Assert.True(rs.Any(role => role == r.Name));
//                }
//            }
//        }


//        [Fact]
//        public async Task RemoveUserFromRoleWithMultipleRoles()
//        {
//            var context = CreateContext();
//            var userManager = CreateManager(context);
//            var roleManager = CreateRoleManager(context);
//            var user = new IdentityUser("MultiRoleUser");
//            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
//            IdentityRole[] roles =
//            {
//                new IdentityRole("r1"), new IdentityRole("r2"), new IdentityRole("r3"),
//                new IdentityRole("r4")
//            };
//            foreach (var r in roles)
//            {
//                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
//                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user.Id, r.Name));
//                Assert.True(await userManager.IsInRoleAsync(user.Id, r.Name));
//            }
//            IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(user.Id, roles[2].Name));
//            Assert.False(await userManager.IsInRoleAsync(user.Id, roles[2].Name));
//        }

//        [Fact]
//        public async Task CanRemoveUsersFromRole()
//        {
//            var context = CreateContext();
//            var userManager = CreateManager(context);
//            var roleManager = CreateRoleManager(context);
//            IdentityUser[] users =
//            {
//                new IdentityUser("1"), new IdentityUser("2"), new IdentityUser("3"),
//                new IdentityUser("4")
//            };
//            foreach (var u in users)
//            {
//                IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
//            }
//            var r = new IdentityRole("r1");
//            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
//            foreach (var u in users)
//            {
//                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u.Id, r.Name));
//                Assert.True(await userManager.IsInRoleAsync(u.Id, r.Name));
//            }
//            foreach (var u in users)
//            {
//                IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(u.Id, r.Name));
//                Assert.False(await userManager.IsInRoleAsync(u.Id, r.Name));
//            }
//        }

//        [Fact]
//        public async Task RemoveUserNotInRoleFails()
//        {
//            var context = CreateContext();
//            var userMgr = CreateManager(context);
//            var roleMgr = CreateRoleManager(context);
//            var role = new IdentityRole("addUserDupeTest");
//            var user = new IdentityUser("user1");
//            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
//            var result = await userMgr.RemoveFromRoleAsync(user.Id, role.Name);
//            IdentityResultAssert.IsFailure(result, "User is not in role.");
//        }

//        [Fact]
//        public async Task AddUserToRoleFailsIfAlreadyInRole()
//        {
//            var context = CreateContext();
//            var userMgr = CreateManager(context);
//            var roleMgr = CreateRoleManager(context);
//            var role = new IdentityRole("addUserDupeTest");
//            var user = new IdentityUser("user1");
//            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
//            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
//            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user.Id, role.Name));
//            Assert.True(await userMgr.IsInRoleAsync(user.Id, role.Name));
//            IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user.Id, role.Name), "User already in role.");
//        }

//        [Fact]
//        public async Task CanFindRoleByNameWithManager()
//        {
//            var roleMgr = CreateRoleManager();
//            var role = new IdentityRole("findRoleByNameTest");
//            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
//            Assert.Equal(role.Id, (await roleMgr.FindByNameAsync(role.Name)).Id);
//        }

//        [Fact]
//        public async Task CanFindRoleWithManager()
//        {
//            var roleMgr = CreateRoleManager();
//            var role = new IdentityRole("findRoleTest");
//            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
//            Assert.Equal(role, await roleMgr.FindByIdAsync(role.Id));
//        }

//        [Fact]
//        public async Task SetPhoneNumberTest()
//        {
//            var manager = CreateManager();
//            var userName = "PhoneTest";
//            var user = new IdentityUser(userName);
//            user.PhoneNumber = "123-456-7890";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var stamp = await manager.GetSecurityStampAsync(user.Id);
//            Assert.Equal(await manager.GetPhoneNumberAsync(user.Id), "123-456-7890");
//            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user.Id, "111-111-1111"));
//            Assert.Equal(await manager.GetPhoneNumberAsync(user.Id), "111-111-1111");
//            Assert.NotEqual(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task CanChangePhoneNumber()
//        {
//            var manager = CreateManager();
//            const string userName = "PhoneTest";
//            var user = new IdentityUser(userName) { PhoneNumber = "123-456-7890" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user.Id));
//            var stamp = await manager.GetSecurityStampAsync(user.Id);
//            var token1 = await manager.GenerateChangePhoneNumberToken(user.Id, "111-111-1111");
//            IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user.Id, "111-111-1111", token1));
//            Assert.True(await manager.IsPhoneNumberConfirmedAsync(user.Id));
//            Assert.Equal(await manager.GetPhoneNumberAsync(user.Id), "111-111-1111");
//            Assert.NotEqual(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task ChangePhoneNumberFailsWithWrongToken()
//        {
//            var manager = CreateManager();
//            const string userName = "PhoneTest";
//            var user = new IdentityUser(userName) { PhoneNumber = "123-456-7890" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user.Id));
//            var stamp = await manager.GetSecurityStampAsync(user.Id);
//            IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user.Id, "111-111-1111", "bogus"),
//                "Invalid token.");
//            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user.Id));
//            Assert.Equal(await manager.GetPhoneNumberAsync(user.Id), "123-456-7890");
//            Assert.Equal(stamp, user.SecurityStamp);
//        }

//        [Fact]
//        public async Task CanVerifyPhoneNumber()
//        {
//            var manager = CreateManager();
//            const string userName = "VerifyPhoneTest";
//            var user = new IdentityUser(userName);
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            const string num1 = "111-123-4567";
//            const string num2 = "111-111-1111";
//            var token1 = await manager.GenerateChangePhoneNumberToken(user.Id, num1);
//            var token2 = await manager.GenerateChangePhoneNumberToken(user.Id, num2);
//            Assert.NotEqual(token1, token2);
//            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user.Id, token1, num1));
//            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user.Id, token2, num2));
//            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user.Id, token2, num1));
//            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user.Id, token1, num2));
//        }

//        private class EmailTokenProvider : IUserTokenProvider<IdentityUser>
//        {
//            public Task<string> GenerateAsync(string purpose, UserManager<IdentityUser> manager, IdentityUser user, CancellationToken token)
//            {
//                return Task.FromResult(MakeToken(purpose));
//            }

//            public Task<bool> ValidateAsync(string purpose, string token, UserManager<IdentityUser> manager,
//                IdentityUser user, CancellationToken cancellationToken)
//            {
//                return Task.FromResult(token == MakeToken(purpose));
//            }

//            public Task NotifyAsync(string token, UserManager<IdentityUser> manager, IdentityUser user, CancellationToken cancellationToken)
//            {
//                return manager.SendEmail(user.Id, token, token);
//            }

//            public async Task<bool> IsValidProviderForUserAsync(UserManager<IdentityUser> manager, IdentityUser user, CancellationToken token)
//            {
//                return !string.IsNullOrEmpty(await manager.GetEmailAsync(user.Id));
//            }

//            private static string MakeToken(string purpose)
//            {
//                return "email:" + purpose;
//            }
//        }

//        private class SmsTokenProvider : IUserTokenProvider<IdentityUser>
//        {
//            public Task<string> GenerateAsync(string purpose, UserManager<IdentityUser> manager, IdentityUser user, CancellationToken token)
//            {
//                return Task.FromResult(MakeToken(purpose));
//            }

//            public Task<bool> ValidateAsync(string purpose, string token, UserManager<IdentityUser> manager,
//                IdentityUser user, CancellationToken cancellationToken)
//            {
//                return Task.FromResult(token == MakeToken(purpose));
//            }

//            public Task NotifyAsync(string token, UserManager<IdentityUser> manager, IdentityUser user, CancellationToken cancellationToken)
//            {
//                return manager.SendSms(user.Id, token);
//            }

//            public async Task<bool> IsValidProviderForUserAsync(UserManager<IdentityUser> manager, IdentityUser user, CancellationToken token)
//            {
//                return !string.IsNullOrEmpty(await manager.GetPhoneNumberAsync(user.Id));
//            }

//            private static string MakeToken(string purpose)
//            {
//                return "sms:" + purpose;
//            }
//        }

//        [Fact]
//        public async Task CanEmailTwoFactorToken()
//        {
//            var manager = CreateManager();
//            var messageService = new TestMessageService();
//            manager.EmailService = messageService;
//            const string factorId = "EmailCode";
//            manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider());
//            var user = new IdentityUser("EmailCodeTest") { Email = "foo@foo.com" };
//            const string password = "password";
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            var token = await manager.GenerateTwoFactorTokenAsync(user.Id, factorId);
//            Assert.NotNull(token);
//            Assert.Null(messageService.Message);
//            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user.Id, factorId, token));
//            Assert.NotNull(messageService.Message);
//            Assert.Equal(token, messageService.Message.Subject);
//            Assert.Equal(token, messageService.Message.Body);
//            Assert.True(await manager.VerifyTwoFactorTokenAsync(user.Id, factorId, token));
//        }

//        [Fact]
//        public async Task NotifyWithUnknownProviderFails()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("NotifyFail");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            await
//                ExceptionAssert.ThrowsAsync<NotSupportedException>(
//                    async () => await manager.NotifyTwoFactorTokenAsync(user.Id, "Bogus", "token"),
//                    "No IUserTwoFactorProvider for 'Bogus' is registered.");
//        }


//        //[Fact]
//        //public async Task EmailTokenFactorWithFormatTest()
//        //{
//        //    var manager = CreateManager();
//        //    var messageService = new TestMessageService();
//        //    manager.EmailService = messageService;
//        //    const string factorId = "EmailCode";
//        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<IdentityUser>
//        //    {
//        //        Subject = "Security Code",
//        //        BodyFormat = "Your code is: {0}"
//        //    });
//        //    var user = new IdentityUser("EmailCodeTest") { Email = "foo@foo.com" };
//        //    const string password = "password";
//        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
//        //    var stamp = user.SecurityStamp;
//        //    Assert.NotNull(stamp);
//        //    var token = await manager.GenerateTwoFactorTokenAsync(user.Id, factorId);
//        //    Assert.NotNull(token);
//        //    Assert.Null(messageService.Message);
//        //    IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user.Id, factorId, token));
//        //    Assert.NotNull(messageService.Message);
//        //    Assert.Equal("Security Code", messageService.Message.Subject);
//        //    Assert.Equal("Your code is: " + token, messageService.Message.Body);
//        //    Assert.True(await manager.VerifyTwoFactorTokenAsync(user.Id, factorId, token));
//        //}

//        //[Fact]
//        //public async Task EmailFactorFailsAfterSecurityStampChangeTest()
//        //{
//        //    var manager = CreateManager();
//        //    const string factorId = "EmailCode";
//        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<IdentityUser>());
//        //    var user = new IdentityUser("EmailCodeTest") { Email = "foo@foo.com" };
//        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//        //    var stamp = user.SecurityStamp;
//        //    Assert.NotNull(stamp);
//        //    var token = await manager.GenerateTwoFactorTokenAsync(user.Id, factorId);
//        //    Assert.NotNull(token);
//        //    IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user.Id));
//        //    Assert.False(await manager.VerifyTwoFactorTokenAsync(user.Id, factorId, token));
//        //}

//        [Fact]
//        public async Task EnableTwoFactorChangesSecurityStamp()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("TwoFactorEnabledTest");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            IdentityResultAssert.IsSuccess(await manager.SetTwoFactorEnabledAsync(user.Id, true));
//            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user.Id));
//            Assert.True(await manager.GetTwoFactorEnabledAsync(user.Id));
//        }

//        [Fact]
//        public async Task CanSendSms()
//        {
//            var manager = CreateManager();
//            var messageService = new TestMessageService();
//            manager.SmsService = messageService;
//            var user = new IdentityUser("SmsTest") { PhoneNumber = "4251234567" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            await manager.SendSms(user.Id, "Hi");
//            Assert.NotNull(messageService.Message);
//            Assert.Equal("Hi", messageService.Message.Body);
//        }

//        [Fact]
//        public async Task CanSendEmail()
//        {
//            var manager = CreateManager();
//            var messageService = new TestMessageService();
//            manager.EmailService = messageService;
//            var user = new IdentityUser("EmailTest") { Email = "foo@foo.com" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            await manager.SendEmail(user.Id, "Hi", "Body");
//            Assert.NotNull(messageService.Message);
//            Assert.Equal("Hi", messageService.Message.Subject);
//            Assert.Equal("Body", messageService.Message.Body);
//        }

//        [Fact]
//        public async Task CanSmsTwoFactorToken()
//        {
//            var manager = CreateManager();
//            var messageService = new TestMessageService();
//            manager.SmsService = messageService;
//            const string factorId = "PhoneCode";
//            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
//            var user = new IdentityUser("PhoneCodeTest") { PhoneNumber = "4251234567" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var stamp = user.SecurityStamp;
//            Assert.NotNull(stamp);
//            var token = await manager.GenerateTwoFactorTokenAsync(user.Id, factorId);
//            Assert.NotNull(token);
//            Assert.Null(messageService.Message);
//            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user.Id, factorId, token));
//            Assert.NotNull(messageService.Message);
//            Assert.Equal(token, messageService.Message.Body);
//            Assert.True(await manager.VerifyTwoFactorTokenAsync(user.Id, factorId, token));
//        }

//        //[Fact]
//        //public async Task PhoneTokenFactorFormatTest()
//        //{
//        //    var manager = CreateManager();
//        //    var messageService = new TestMessageService();
//        //    manager.SmsService = messageService;
//        //    const string factorId = "PhoneCode";
//        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<IdentityUser>
//        //    {
//        //        MessageFormat = "Your code is: {0}"
//        //    });
//        //    var user = new IdentityUser("PhoneCodeTest") { PhoneNumber = "4251234567" };
//        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//        //    var stamp = user.SecurityStamp;
//        //    Assert.NotNull(stamp);
//        //    var token = await manager.GenerateTwoFactorTokenAsync(user.Id, factorId);
//        //    Assert.NotNull(token);
//        //    Assert.Null(messageService.Message);
//        //    IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user.Id, factorId, token));
//        //    Assert.NotNull(messageService.Message);
//        //    Assert.Equal("Your code is: " + token, messageService.Message.Body);
//        //    Assert.True(await manager.VerifyTwoFactorTokenAsync(user.Id, factorId, token));
//        //}

//        [Fact]
//        public async Task GenerateTwoFactorWithUnknownFactorProviderWillThrow()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("PhoneCodeTest");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            const string error = "No IUserTwoFactorProvider for 'bogus' is registered.";
//            await
//                ExceptionAssert.ThrowsAsync<NotSupportedException>(
//                    () => manager.GenerateTwoFactorTokenAsync(user.Id, "bogus"), error);
//            await ExceptionAssert.ThrowsAsync<NotSupportedException>(
//                () => manager.VerifyTwoFactorTokenAsync(user.Id, "bogus", "bogus"), error);
//        }

//        [Fact]
//        public async Task GetValidTwoFactorTestEmptyWithNoProviders()
//        {
//            var manager = CreateManager();
//            var user = new IdentityUser("test");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var factors = await manager.GetValidTwoFactorProviders(user.Id);
//            Assert.NotNull(factors);
//            Assert.True(!factors.Any());
//        }

//        [Fact]
//        public async Task GetValidTwoFactorTest()
//        {
//            var manager = CreateManager();
//            manager.RegisterTwoFactorProvider("phone", new SmsTokenProvider());
//            manager.RegisterTwoFactorProvider("email", new EmailTokenProvider());
//            var user = new IdentityUser("test");
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var factors = await manager.GetValidTwoFactorProviders(user.Id);
//            Assert.NotNull(factors);
//            Assert.True(!factors.Any());
//            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user.Id, "111-111-1111"));
//            factors = await manager.GetValidTwoFactorProviders(user.Id);
//            Assert.NotNull(factors);
//            Assert.True(factors.Count() == 1);
//            Assert.Equal("phone", factors[0]);
//            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user.Id, "test@test.com"));
//            factors = await manager.GetValidTwoFactorProviders(user.Id);
//            Assert.NotNull(factors);
//            Assert.True(factors.Count() == 2);
//            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user.Id, null));
//            factors = await manager.GetValidTwoFactorProviders(user.Id);
//            Assert.NotNull(factors);
//            Assert.True(factors.Count() == 1);
//            Assert.Equal("phone", factors[0]);
//        }

//        //[Fact]
//        //public async Task PhoneFactorFailsAfterSecurityStampChangeTest()
//        //{
//        //    var manager = CreateManager();
//        //    var factorId = "PhoneCode";
//        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<IdentityUser>());
//        //    var user = new IdentityUser("PhoneCodeTest");
//        //    user.PhoneNumber = "4251234567";
//        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//        //    var stamp = user.SecurityStamp;
//        //    Assert.NotNull(stamp);
//        //    var token = await manager.GenerateTwoFactorTokenAsync(user.Id, factorId);
//        //    Assert.NotNull(token);
//        //    IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user.Id));
//        //    Assert.False(await manager.VerifyTwoFactorTokenAsync(user.Id, factorId, token));
//        //}

//        [Fact]
//        public async Task VerifyTokenFromWrongTokenProviderFails()
//        {
//            var manager = CreateManager();
//            manager.RegisterTwoFactorProvider("PhoneCode", new SmsTokenProvider());
//            manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider());
//            var user = new IdentityUser("WrongTokenProviderTest") { PhoneNumber = "4251234567" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            var token = await manager.GenerateTwoFactorTokenAsync(user.Id, "PhoneCode");
//            Assert.NotNull(token);
//            Assert.False(await manager.VerifyTwoFactorTokenAsync(user.Id, "EmailCode", token));
//        }

//        [Fact]
//        public async Task VerifyWithWrongSmsTokenFails()
//        {
//            var manager = CreateManager();
//            const string factorId = "PhoneCode";
//            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
//            var user = new IdentityUser("PhoneCodeTest") { PhoneNumber = "4251234567" };
//            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
//            Assert.False(await manager.VerifyTwoFactorTokenAsync(user.Id, factorId, "bogus"));
//        }

//        private class DataStoreConfig : ContextConfiguration
//        {
//            private readonly DataStore _store;

//            public DataStoreConfig(DataStore store)
//            {
//                _store = store;
//            }

//            public override DataStore DataStore
//            {
//                get { return _store; }
//            }

//        }

//        private static EntityContext CreateContext()
//        {
//            var configuration = new EntityConfigurationBuilder()
//                            //.UseModel(model)
//                            .UseDataStore(new InMemoryDataStore())
//                            .BuildConfiguration();

//            var db = new IdentityContext(configuration);
//            //            var sql = db.Configuration.DataStore as SqlServerDataStore;
//            //            if (sql != null)
//            //            {
//            //#if NET45
//            //                var builder = new DbConnectionStringBuilder {ConnectionString = sql.ConnectionString};
//            //                var targetDatabase = builder["Database"].ToString();

//            //                // Connect to master, check if database exists, and create if not
//            //                builder.Add("Database", "master");
//            //                using (var masterConnection = new SqlConnection(builder.ConnectionString))
//            //                {
//            //                    masterConnection.Open();

//            //                    var masterCommand = masterConnection.CreateCommand();
//            //                    masterCommand.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE [name]=N'" + targetDatabase +
//            //                                                "'";
//            //                    if ((int?) masterCommand.ExecuteScalar() < 1)
//            //                    {
//            //                        masterCommand.CommandText = "CREATE DATABASE [" + targetDatabase + "]";
//            //                        masterCommand.ExecuteNonQuery();

//            //                        using (var conn = new SqlConnection(sql.ConnectionString))
//            //                        {
//            //                            conn.Open();
//            //                            var command = conn.CreateCommand();
//            //                            command.CommandText = @"
//            //CREATE TABLE [dbo].[AspNetUsers] (
//            //[Id]                   NVARCHAR (128) NOT NULL,
//            //[Email]                NVARCHAR (256) NULL,
//            //[EmailConfirmed]       BIT            NOT NULL,
//            //[PasswordHash]         NVARCHAR (MAX) NULL,
//            //[SecurityStamp]        NVARCHAR (MAX) NULL,
//            //[PhoneNumber]          NVARCHAR (MAX) NULL,
//            //[PhoneNumberConfirmed] BIT            NOT NULL,
//            //[TwoFactorEnabled]     BIT            NOT NULL,
//            //[LockoutEndDateUtc]    DATETIME       NULL,
//            //[LockoutEnabled]       BIT            NOT NULL,
//            //[AccessFailedCount]    INT            NOT NULL,
//            //[UserName]             NVARCHAR (256) NOT NULL
//            //) ";
//            //                            //CONSTRAINT [PK_dbo.AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
//            //                            command.ExecuteNonQuery();
//            //                        }
//            //                    }
//            //                }
//            //#else
//            //                throw new NotSupportedException("SQL Server is not yet supported when running against K10.");
//            //#endif
//            //}


//            // TODO: CreateAsync DB?
//            return db;
//        }


//        private static UserManager<IdentityUser> CreateManager(EntityContext context)
//        {
//            return new UserManager<IdentityUser>(new UserStore(context));
//        }

//        private static UserManager<IdentityUser> CreateManager()
//        {
//            return CreateManager(CreateContext());
//        }

//        private static RoleManager<IdentityRole> CreateRoleManager(EntityContext context)
//        {
//            return new RoleManager<IdentityRole>(new RoleStore<IdentityRole, string>(context));
//        }

//        private static RoleManager<IdentityRole> CreateRoleManager()
//        {
//            return CreateRoleManager(CreateContext());
//        }

//        public class TestMessageService : IIdentityMessageService
//        {
//            public IdentityMessage Message { get; set; }

//            public Task SendAsync(IdentityMessage message, CancellationToken token)
//            {
//                Message = message;
//                return Task.FromResult(0);
//            }
//        }
//    }
//}
