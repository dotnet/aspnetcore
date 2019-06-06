// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore
{
    public partial class IdentityDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<Microsoft.AspNetCore.Identity.IdentityUser, Microsoft.AspNetCore.Identity.IdentityRole, string>
    {
        protected IdentityDbContext() { }
        public IdentityDbContext(Microsoft.EntityFrameworkCore.DbContextOptions options) { }
    }
    public partial class IdentityDbContext<TUser> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<TUser, Microsoft.AspNetCore.Identity.IdentityRole, string> where TUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        protected IdentityDbContext() { }
        public IdentityDbContext(Microsoft.EntityFrameworkCore.DbContextOptions options) { }
    }
    public partial class IdentityDbContext<TUser, TRole, TKey> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<TUser, TRole, TKey, Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>, Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>, Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TKey : System.IEquatable<TKey>
    {
        protected IdentityDbContext() { }
        public IdentityDbContext(Microsoft.EntityFrameworkCore.DbContextOptions options) { }
    }
    public abstract partial class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TKey : System.IEquatable<TKey> where TUserClaim : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey> where TUserRole : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey> where TUserLogin : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey> where TRoleClaim : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey> where TUserToken : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>
    {
        protected IdentityDbContext() { }
        public IdentityDbContext(Microsoft.EntityFrameworkCore.DbContextOptions options) { }
        public virtual Microsoft.EntityFrameworkCore.DbSet<TRoleClaim> RoleClaims { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.EntityFrameworkCore.DbSet<TRole> Roles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.EntityFrameworkCore.DbSet<TUserRole> UserRoles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder builder) { }
    }
    public partial class IdentityUserContext<TUser> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserContext<TUser, string> where TUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        protected IdentityUserContext() { }
        public IdentityUserContext(Microsoft.EntityFrameworkCore.DbContextOptions options) { }
    }
    public partial class IdentityUserContext<TUser, TKey> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserContext<TUser, TKey, Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TKey : System.IEquatable<TKey>
    {
        protected IdentityUserContext() { }
        public IdentityUserContext(Microsoft.EntityFrameworkCore.DbContextOptions options) { }
    }
    public abstract partial class IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken> : Microsoft.EntityFrameworkCore.DbContext where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TKey : System.IEquatable<TKey> where TUserClaim : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey> where TUserLogin : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey> where TUserToken : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>
    {
        protected IdentityUserContext() { }
        public IdentityUserContext(Microsoft.EntityFrameworkCore.DbContextOptions options) { }
        public virtual Microsoft.EntityFrameworkCore.DbSet<TUserClaim> UserClaims { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.EntityFrameworkCore.DbSet<TUserLogin> UserLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.EntityFrameworkCore.DbSet<TUser> Users { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.EntityFrameworkCore.DbSet<TUserToken> UserTokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder builder) { }
    }
    public partial class RoleStore<TRole> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<TRole, Microsoft.EntityFrameworkCore.DbContext, string> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<string>
    {
        public RoleStore(Microsoft.EntityFrameworkCore.DbContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(Microsoft.EntityFrameworkCore.DbContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class RoleStore<TRole, TContext> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<TRole, TContext, string> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<string> where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public RoleStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(TContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class RoleStore<TRole, TContext, TKey> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<TRole, TContext, TKey, Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>, Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>>, Microsoft.AspNetCore.Identity.IQueryableRoleStore<TRole>, Microsoft.AspNetCore.Identity.IRoleClaimStore<TRole>, Microsoft.AspNetCore.Identity.IRoleStore<TRole>, System.IDisposable where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TContext : Microsoft.EntityFrameworkCore.DbContext where TKey : System.IEquatable<TKey>
    {
        public RoleStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(TContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class RoleStore<TRole, TContext, TKey, TUserRole, TRoleClaim> : Microsoft.AspNetCore.Identity.IQueryableRoleStore<TRole>, Microsoft.AspNetCore.Identity.IRoleClaimStore<TRole>, Microsoft.AspNetCore.Identity.IRoleStore<TRole>, System.IDisposable where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TContext : Microsoft.EntityFrameworkCore.DbContext where TKey : System.IEquatable<TKey> where TUserRole : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>, new() where TRoleClaim : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>, new()
    {
        public RoleStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) { }
        public bool AutoSaveChanges { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual TContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Identity.IdentityErrorDescriber ErrorDescriber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Linq.IQueryable<TRole> Roles { get { throw null; } }
        public virtual System.Threading.Tasks.Task AddClaimAsync(TRole role, System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual TKey ConvertIdFromString(string id) { throw null; }
        public virtual string ConvertIdToString(TKey id) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected virtual TRoleClaim CreateRoleClaim(TRole role, System.Security.Claims.Claim claim) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Dispose() { }
        public virtual System.Threading.Tasks.Task<TRole> FindByIdAsync(string id, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<TRole> FindByNameAsync(string normalizedName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetNormalizedRoleNameAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetRoleIdAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetRoleNameAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task RemoveClaimAsync(TRole role, System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetRoleNameAsync(TRole role, string roleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected void ThrowIfDisposed() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class UserOnlyStore<TUser> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserOnlyStore<TUser, Microsoft.EntityFrameworkCore.DbContext, string> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<string>, new()
    {
        public UserOnlyStore(Microsoft.EntityFrameworkCore.DbContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(Microsoft.EntityFrameworkCore.DbContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class UserOnlyStore<TUser, TContext> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserOnlyStore<TUser, TContext, string> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<string> where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public UserOnlyStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(TContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class UserOnlyStore<TUser, TContext, TKey> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserOnlyStore<TUser, TContext, TKey, Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TContext : Microsoft.EntityFrameworkCore.DbContext where TKey : System.IEquatable<TKey>
    {
        public UserOnlyStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(TContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken> : Microsoft.AspNetCore.Identity.UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>, Microsoft.AspNetCore.Identity.IProtectedUserStore<TUser>, Microsoft.AspNetCore.Identity.IQueryableUserStore<TUser>, Microsoft.AspNetCore.Identity.IUserAuthenticationTokenStore<TUser>, Microsoft.AspNetCore.Identity.IUserAuthenticatorKeyStore<TUser>, Microsoft.AspNetCore.Identity.IUserClaimStore<TUser>, Microsoft.AspNetCore.Identity.IUserEmailStore<TUser>, Microsoft.AspNetCore.Identity.IUserLockoutStore<TUser>, Microsoft.AspNetCore.Identity.IUserLoginStore<TUser>, Microsoft.AspNetCore.Identity.IUserPasswordStore<TUser>, Microsoft.AspNetCore.Identity.IUserPhoneNumberStore<TUser>, Microsoft.AspNetCore.Identity.IUserSecurityStampStore<TUser>, Microsoft.AspNetCore.Identity.IUserStore<TUser>, Microsoft.AspNetCore.Identity.IUserTwoFactorRecoveryCodeStore<TUser>, Microsoft.AspNetCore.Identity.IUserTwoFactorStore<TUser>, System.IDisposable where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TContext : Microsoft.EntityFrameworkCore.DbContext where TKey : System.IEquatable<TKey> where TUserClaim : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, new() where TUserLogin : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, new() where TUserToken : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>, new()
    {
        public UserOnlyStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
        public bool AutoSaveChanges { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual TContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected Microsoft.EntityFrameworkCore.DbSet<TUserClaim> UserClaims { get { throw null; } }
        protected Microsoft.EntityFrameworkCore.DbSet<TUserLogin> UserLogins { get { throw null; } }
        public override System.Linq.IQueryable<TUser> Users { get { throw null; } }
        protected Microsoft.EntityFrameworkCore.DbSet<TUser> UsersSet { get { throw null; } }
        protected Microsoft.EntityFrameworkCore.DbSet<TUserToken> UserTokens { get { throw null; } }
        public override System.Threading.Tasks.Task AddClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task AddLoginAsync(TUser user, Microsoft.AspNetCore.Identity.UserLoginInfo login, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.Tasks.Task AddUserTokenAsync(TUserToken token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task<TUser> FindByEmailAsync(string normalizedEmail, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task<TUser> FindByIdAsync(string userId, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task<TUser> FindByNameAsync(string normalizedUserName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.Tasks.Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUser> FindUserAsync(TKey userId, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.UserLoginInfo>> GetLoginsAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersForClaimAsync(System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task RemoveClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.Tasks.Task RemoveUserTokenAsync(TUserToken token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ReplaceClaimAsync(TUser user, System.Security.Claims.Claim claim, System.Security.Claims.Claim newClaim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected System.Threading.Tasks.Task SaveChanges(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class UserStore : Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<Microsoft.AspNetCore.Identity.IdentityUser<string>>
    {
        public UserStore(Microsoft.EntityFrameworkCore.DbContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(Microsoft.EntityFrameworkCore.DbContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class UserStore<TUser> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<TUser, Microsoft.AspNetCore.Identity.IdentityRole, Microsoft.EntityFrameworkCore.DbContext, string> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<string>, new()
    {
        public UserStore(Microsoft.EntityFrameworkCore.DbContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(Microsoft.EntityFrameworkCore.DbContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class UserStore<TUser, TRole, TContext> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<TUser, TRole, TContext, string> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<string> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<string> where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public UserStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(TContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class UserStore<TUser, TRole, TContext, TKey> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<TUser, TRole, TContext, TKey, Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>, Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>, Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>> where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TContext : Microsoft.EntityFrameworkCore.DbContext where TKey : System.IEquatable<TKey>
    {
        public UserStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(TContext), default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
    }
    public partial class UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> : Microsoft.AspNetCore.Identity.UserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>, Microsoft.AspNetCore.Identity.IProtectedUserStore<TUser>, Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TContext : Microsoft.EntityFrameworkCore.DbContext where TKey : System.IEquatable<TKey> where TUserClaim : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, new() where TUserRole : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>, new() where TUserLogin : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, new() where TUserToken : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>, new() where TRoleClaim : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>, new()
    {
        public UserStore(TContext context, Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer = null) : base (default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
        public bool AutoSaveChanges { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual TContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override System.Linq.IQueryable<TUser> Users { get { throw null; } }
        public override System.Threading.Tasks.Task AddClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task AddLoginAsync(TUser user, Microsoft.AspNetCore.Identity.UserLoginInfo login, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task AddToRoleAsync(TUser user, string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.Tasks.Task AddUserTokenAsync(TUserToken token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task<TUser> FindByEmailAsync(string normalizedEmail, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task<TUser> FindByIdAsync(string userId, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task<TUser> FindByNameAsync(string normalizedUserName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.Tasks.Task<TRole> FindRoleAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUser> FindUserAsync(TKey userId, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<TUserRole> FindUserRoleAsync(TKey userId, TKey roleId, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.UserLoginInfo>> GetLoginsAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<string>> GetRolesAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersForClaimAsync(System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task RemoveClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.Tasks.Task RemoveUserTokenAsync(TUserToken token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ReplaceClaimAsync(TUser user, System.Security.Claims.Claim claim, System.Security.Claims.Claim newClaim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected System.Threading.Tasks.Task SaveChanges(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class IdentityEntityFrameworkBuilderExtensions
    {
        public static Microsoft.AspNetCore.Identity.IdentityBuilder AddEntityFrameworkStores<TContext>(this Microsoft.AspNetCore.Identity.IdentityBuilder builder) where TContext : Microsoft.EntityFrameworkCore.DbContext { throw null; }
    }
}
