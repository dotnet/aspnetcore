# Microsoft.AspNetCore.Identity.EntityFrameworkCore

``` diff
-namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore {
 {
-    public class IdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string> {
 {
-        protected IdentityDbContext();

-        public IdentityDbContext(DbContextOptions options);

-    }
-    public class IdentityDbContext<TUser> : IdentityDbContext<TUser, IdentityRole, string> where TUser : IdentityUser {
 {
-        protected IdentityDbContext();

-        public IdentityDbContext(DbContextOptions options);

-    }
-    public class IdentityDbContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>> where TUser : IdentityUser<TKey> where TRole : IdentityRole<TKey> where TKey : IEquatable<TKey> {
 {
-        protected IdentityDbContext();

-        public IdentityDbContext(DbContextOptions options);

-    }
-    public abstract class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken> where TUser : IdentityUser<TKey> where TRole : IdentityRole<TKey> where TKey : IEquatable<TKey> where TUserClaim : IdentityUserClaim<TKey> where TUserRole : IdentityUserRole<TKey> where TUserLogin : IdentityUserLogin<TKey> where TRoleClaim : IdentityRoleClaim<TKey> where TUserToken : IdentityUserToken<TKey> {
 {
-        protected IdentityDbContext();

-        public IdentityDbContext(DbContextOptions options);

-        public DbSet<TRoleClaim> RoleClaims { get; set; }

-        public DbSet<TRole> Roles { get; set; }

-        public DbSet<TUserRole> UserRoles { get; set; }

-        protected override void OnModelCreating(ModelBuilder builder);

-    }
-    public class IdentityUserContext<TUser> : IdentityUserContext<TUser, string> where TUser : IdentityUser {
 {
-        protected IdentityUserContext();

-        public IdentityUserContext(DbContextOptions options);

-    }
-    public class IdentityUserContext<TUser, TKey> : IdentityUserContext<TUser, TKey, IdentityUserClaim<TKey>, IdentityUserLogin<TKey>, IdentityUserToken<TKey>> where TUser : IdentityUser<TKey> where TKey : IEquatable<TKey> {
 {
-        protected IdentityUserContext();

-        public IdentityUserContext(DbContextOptions options);

-    }
-    public abstract class IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken> : DbContext where TUser : IdentityUser<TKey> where TKey : IEquatable<TKey> where TUserClaim : IdentityUserClaim<TKey> where TUserLogin : IdentityUserLogin<TKey> where TUserToken : IdentityUserToken<TKey> {
 {
-        protected IdentityUserContext();

-        public IdentityUserContext(DbContextOptions options);

-        public DbSet<TUserClaim> UserClaims { get; set; }

-        public DbSet<TUserLogin> UserLogins { get; set; }

-        public DbSet<TUser> Users { get; set; }

-        public DbSet<TUserToken> UserTokens { get; set; }

-        protected override void OnModelCreating(ModelBuilder builder);

-    }
-    public class RoleStore<TRole> : RoleStore<TRole, DbContext, string> where TRole : IdentityRole<string> {
 {
-        public RoleStore(DbContext context, IdentityErrorDescriber describer = null);

-    }
-    public class RoleStore<TRole, TContext> : RoleStore<TRole, TContext, string> where TRole : IdentityRole<string> where TContext : DbContext {
 {
-        public RoleStore(TContext context, IdentityErrorDescriber describer = null);

-    }
-    public class RoleStore<TRole, TContext, TKey> : RoleStore<TRole, TContext, TKey, IdentityUserRole<TKey>, IdentityRoleClaim<TKey>>, IDisposable, IQueryableRoleStore<TRole>, IRoleClaimStore<TRole>, IRoleStore<TRole> where TRole : IdentityRole<TKey> where TContext : DbContext where TKey : IEquatable<TKey> {
 {
-        public RoleStore(TContext context, IdentityErrorDescriber describer = null);

-    }
-    public class RoleStore<TRole, TContext, TKey, TUserRole, TRoleClaim> : IDisposable, IQueryableRoleStore<TRole>, IRoleClaimStore<TRole>, IRoleStore<TRole> where TRole : IdentityRole<TKey> where TContext : DbContext where TKey : IEquatable<TKey> where TUserRole : IdentityUserRole<TKey>, new() where TRoleClaim : IdentityRoleClaim<TKey>, new() {
 {
-        public RoleStore(TContext context, IdentityErrorDescriber describer = null);

-        public bool AutoSaveChanges { get; set; }

-        public TContext Context { get; private set; }

-        public IdentityErrorDescriber ErrorDescriber { get; set; }

-        public virtual IQueryable<TRole> Roles { get; }

-        public virtual Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual TKey ConvertIdFromString(string id);

-        public virtual string ConvertIdToString(TKey id);

-        public virtual Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual TRoleClaim CreateRoleClaim(TRole role, Claim claim);

-        public virtual Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

-        public void Dispose();

-        public virtual Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken));

-        protected void ThrowIfDisposed();

-        public virtual Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class UserOnlyStore<TUser> : UserOnlyStore<TUser, DbContext, string> where TUser : IdentityUser<string>, new() {
 {
-        public UserOnlyStore(DbContext context, IdentityErrorDescriber describer = null);

-    }
-    public class UserOnlyStore<TUser, TContext> : UserOnlyStore<TUser, TContext, string> where TUser : IdentityUser<string> where TContext : DbContext {
 {
-        public UserOnlyStore(TContext context, IdentityErrorDescriber describer = null);

-    }
-    public class UserOnlyStore<TUser, TContext, TKey> : UserOnlyStore<TUser, TContext, TKey, IdentityUserClaim<TKey>, IdentityUserLogin<TKey>, IdentityUserToken<TKey>> where TUser : IdentityUser<TKey> where TContext : DbContext where TKey : IEquatable<TKey> {
 {
-        public UserOnlyStore(TContext context, IdentityErrorDescriber describer = null);

-    }
-    public class UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken> : UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>, IDisposable, IProtectedUserStore<TUser>, IQueryableUserStore<TUser>, IUserAuthenticationTokenStore<TUser>, IUserAuthenticatorKeyStore<TUser>, IUserClaimStore<TUser>, IUserEmailStore<TUser>, IUserLockoutStore<TUser>, IUserLoginStore<TUser>, IUserPasswordStore<TUser>, IUserPhoneNumberStore<TUser>, IUserSecurityStampStore<TUser>, IUserStore<TUser>, IUserTwoFactorRecoveryCodeStore<TUser>, IUserTwoFactorStore<TUser> where TUser : IdentityUser<TKey> where TContext : DbContext where TKey : IEquatable<TKey> where TUserClaim : IdentityUserClaim<TKey>, new() where TUserLogin : IdentityUserLogin<TKey>, new() where TUserToken : IdentityUserToken<TKey>, new() {
 {
-        public UserOnlyStore(TContext context, IdentityErrorDescriber describer = null);

-        public bool AutoSaveChanges { get; set; }

-        public TContext Context { get; private set; }

-        protected DbSet<TUserClaim> UserClaims { get; }

-        protected DbSet<TUserLogin> UserLogins { get; }

-        public override IQueryable<TUser> Users { get; }

-        protected DbSet<TUser> UsersSet { get; }

-        protected DbSet<TUserToken> UserTokens { get; }

-        public override Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task AddUserTokenAsync(TUserToken token);

-        public override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken);

-        protected override Task<TUser> FindUserAsync(TKey userId, CancellationToken cancellationToken);

-        protected override Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken);

-        protected override Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken);

-        public override Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task RemoveUserTokenAsync(TUserToken token);

-        public override Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken));

-        protected Task SaveChanges(CancellationToken cancellationToken);

-        public override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class UserStore : UserStore<IdentityUser<string>> {
 {
-        public UserStore(DbContext context, IdentityErrorDescriber describer = null);

-    }
-    public class UserStore<TUser> : UserStore<TUser, IdentityRole, DbContext, string> where TUser : IdentityUser<string>, new() {
 {
-        public UserStore(DbContext context, IdentityErrorDescriber describer = null);

-    }
-    public class UserStore<TUser, TRole, TContext> : UserStore<TUser, TRole, TContext, string> where TUser : IdentityUser<string> where TRole : IdentityRole<string> where TContext : DbContext {
 {
-        public UserStore(TContext context, IdentityErrorDescriber describer = null);

-    }
-    public class UserStore<TUser, TRole, TContext, TKey> : UserStore<TUser, TRole, TContext, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityUserToken<TKey>, IdentityRoleClaim<TKey>> where TUser : IdentityUser<TKey> where TRole : IdentityRole<TKey> where TContext : DbContext where TKey : IEquatable<TKey> {
 {
-        public UserStore(TContext context, IdentityErrorDescriber describer = null);

-    }
-    public class UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> : UserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>, IDisposable, IProtectedUserStore<TUser>, IUserStore<TUser> where TUser : IdentityUser<TKey> where TRole : IdentityRole<TKey> where TContext : DbContext where TKey : IEquatable<TKey> where TUserClaim : IdentityUserClaim<TKey>, new() where TUserRole : IdentityUserRole<TKey>, new() where TUserLogin : IdentityUserLogin<TKey>, new() where TUserToken : IdentityUserToken<TKey>, new() where TRoleClaim : IdentityRoleClaim<TKey>, new() {
 {
-        public UserStore(TContext context, IdentityErrorDescriber describer = null);

-        public bool AutoSaveChanges { get; set; }

-        public TContext Context { get; private set; }

-        public override IQueryable<TUser> Users { get; }

-        public override Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task AddUserTokenAsync(TUserToken token);

-        public override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task<TRole> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken);

-        protected override Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken);

-        protected override Task<TUser> FindUserAsync(TKey userId, CancellationToken cancellationToken);

-        protected override Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken);

-        protected override Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken);

-        protected override Task<TUserRole> FindUserRoleAsync(TKey userId, TKey roleId, CancellationToken cancellationToken);

-        public override Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task RemoveUserTokenAsync(TUserToken token);

-        public override Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken));

-        protected Task SaveChanges(CancellationToken cancellationToken);

-        public override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

-    }
-}
```

