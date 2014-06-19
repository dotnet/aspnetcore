// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Entity;

namespace Microsoft.AspNet.Identity.Entity
{
    public class UserStore<TUser> : UserStore<TUser, DbContext> where TUser : User
    {
        public UserStore(DbContext context) : base(context) { }
    }

    public class UserStore<TUser, TContext> :
        //IUserRoleStore<TUser>,
        IUserPasswordStore<TUser>,
        IQueryableUserStore<TUser>,
        IUserClaimStore<TUser>
        where TUser : User
        where TContext : DbContext
    {
        private bool _disposed;

        public UserStore(TContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            Context = context;
            AutoSaveChanges = true;
        }

        public TContext Context { get; private set; }

        /// <summary>
        ///     If true will call SaveChanges after CreateAsync/UpdateAsync/DeleteAsync
        /// </summary>
        public bool AutoSaveChanges { get; set; }

        private Task SaveChanges(CancellationToken cancellationToken)
        {
            return AutoSaveChanges ? Context.SaveChangesAsync(cancellationToken) : Task.FromResult(0);
        }

        protected virtual Task<TUser> GetUserAggregate(Expression<Func<TUser, bool>> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(Users.SingleOrDefault(filter));
            // TODO: return Users.SingleOrDefaultAsync(filter, cancellationToken);
                //Include(u => u.Roles)
                //.Include(u => u.Claims)
                //.Include(u => u.Logins)
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(Convert.ToString(user.Id, CultureInfo.InvariantCulture));
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.UserName = userName;
            return Task.FromResult(0);
        }

        public async virtual Task CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await Context.AddAsync(user, cancellationToken);
            await SaveChanges(cancellationToken);
        }

        public async virtual Task UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await Context.UpdateAsync(user, cancellationToken);
            await SaveChanges(cancellationToken);
        }

        public async virtual Task DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            Context.Delete(user);
            await SaveChanges(cancellationToken);
        }

        /// <summary>
        ///     Find a user by id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return GetUserAggregate(u => u.Id.Equals(userId), cancellationToken);
        }

        /// <summary>
        ///     Find a user by name
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return GetUserAggregate(u => u.UserName.ToUpper() == userName.ToUpper(), cancellationToken);
        }

        public IQueryable<TUser> Users
        {
            get { return Context.Set<TUser>(); }
        }

        /// <summary>
        ///     Set the password hash for a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="passwordHash"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        /// <summary>
        ///     Get the password hash for a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.PasswordHash);
        }

        /// <summary>
        ///     Returns true if the user has a password set
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(user.PasswordHash != null);
        }

        ///// <summary>
        /////     Add a user to a role
        ///// </summary>
        ///// <param name="user"></param>
        ///// <param name="roleName"></param>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public virtual Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    ThrowIfDisposed();
        //    if (user == null)
        //    {
        //        throw new ArgumentNullException("user");
        //    }
        //    // TODO:
        //    //if (String.IsNullOrWhiteSpace(roleName))
        //    //{
        //    //    throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
        //    //}
        //    var roleEntity = Context.Set<TRole>().SingleOrDefault(r => r.Name.ToUpper() == roleName.ToUpper());
        //    if (roleEntity == null)
        //    {
        //        throw new InvalidOperationException("Role Not Found");
        //        //TODO: String.Format(CultureInfo.CurrentCulture, IdentityResources.RoleNotFound, roleName));
        //    }
        //    var ur = new TUserRole { UserId = user.Id, RoleId = roleEntity.Id };
        //    user.Roles.Add(ur);
        //    roleEntity.Users.Add(ur);
        //    return Task.FromResult(0);
        //}

        ///// <summary>
        /////     Remove a user from a role
        ///// </summary>
        ///// <param name="user"></param>
        ///// <param name="roleName"></param>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public virtual Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    ThrowIfDisposed();
        //    if (user == null)
        //    {
        //        throw new ArgumentNullException("user");
        //    }
        //    //if (String.IsNullOrWhiteSpace(roleName))
        //    //{
        //    //    throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
        //    //}
        //    var roleEntity = Context.Set<TRole>().SingleOrDefault(r => r.Name.ToUpper() == roleName.ToUpper());
        //    if (roleEntity != null)
        //    {
        //        var userRole = user.Roles.FirstOrDefault(r => roleEntity.Id.Equals(r.RoleId));
        //        if (userRole != null)
        //        {
        //            user.Roles.Remove(userRole);
        //            roleEntity.Users.Remove(userRole);
        //        }
        //    }
        //    return Task.FromResult(0);
        //}

        ///// <summary>
        /////     Get the names of the roles a user is a member of
        ///// </summary>
        ///// <param name="user"></param>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public virtual Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    ThrowIfDisposed();
        //    if (user == null)
        //    {
        //        throw new ArgumentNullException("user");
        //    }
        //    var query = from userRoles in user.Roles
        //                join roles in Context.Set<TRole>()
        //                    on userRoles.RoleId equals roles.Id
        //                select roles.Name;
        //    return Task.FromResult<IList<string>>(query.ToList());
        //}

        ///// <summary>
        /////     Returns true if the user is in the named role
        ///// </summary>
        ///// <param name="user"></param>
        ///// <param name="roleName"></param>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public virtual Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    ThrowIfDisposed();
        //    if (user == null)
        //    {
        //        throw new ArgumentNullException("user");
        //    }
        //    //if (String.IsNullOrWhiteSpace(roleName))
        //    //{
        //    //    throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
        //    //}
        //    var any =
        //        Context.Set<TRole>().Where(r => r.Name.ToUpper() == roleName.ToUpper())
        //            .Where(r => r.Users.Any(ur => ur.UserId.Equals(user.Id)))
        //            .Count() > 0;
        //    return Task.FromResult(any);
        //}
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        ///     Dispose the store
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
        }

        private DbSet<IdentityUserClaim> UserClaims { get { return Context.Set<IdentityUserClaim>(); } }

        public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = new CancellationToken())
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            IList<Claim> result = UserClaims.Where(uc => uc.UserId == user.Id).Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
            return Task.FromResult(result);
        }

        public Task AddClaimAsync(TUser user, Claim claim, CancellationToken cancellationToken = new CancellationToken())
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            UserClaims.Add(new IdentityUserClaim { UserId = user.Id, ClaimType = claim.Type, ClaimValue = claim.Value });
            return Task.FromResult(0);
        }

        public Task RemoveClaimAsync(TUser user, Claim claim, CancellationToken cancellationToken = new CancellationToken())
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            var claims = UserClaims.Where(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList();
            foreach (var c in claims)
            {
                UserClaims.Remove(c);
            }
            return Task.FromResult(0);
        }
    }
}
