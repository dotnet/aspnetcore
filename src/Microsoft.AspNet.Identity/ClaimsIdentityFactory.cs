// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Creates a ClaimsIdentity from a User
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class ClaimsIdentityFactory<TUser, TRole> : IClaimsIdentityFactory<TUser>
        where TUser : class
        where TRole : class
    {
        public ClaimsIdentityFactory(UserManager<TUser> userManager, RoleManager<TRole> roleManager)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException("userManager");
            }
            if (roleManager == null)
            {
                throw new ArgumentNullException("roleManager");
            }
            UserManager = userManager;
            RoleManager = roleManager;
        }

        public UserManager<TUser> UserManager { get; private set; }
        public RoleManager<TRole> RoleManager { get; private set; }

        /// <summary>
        ///     CreateAsync a ClaimsIdentity from a user
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<ClaimsIdentity> CreateAsync(TUser user,
            string authenticationType, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var userId = await UserManager.GetUserIdAsync(user, cancellationToken);
            var userName = await UserManager.GetUserNameAsync(user, cancellationToken);
            var id = new ClaimsIdentity(authenticationType, UserManager.Options.ClaimType.UserName, 
                UserManager.Options.ClaimType.Role);
            id.AddClaim(new Claim(UserManager.Options.ClaimType.UserId, userId));
            id.AddClaim(new Claim(UserManager.Options.ClaimType.UserName, userName, ClaimValueTypes.String));
            if (UserManager.SupportsUserSecurityStamp)
            {
                id.AddClaim(new Claim(UserManager.Options.ClaimType.SecurityStamp, 
                    await UserManager.GetSecurityStampAsync(user, cancellationToken)));
            }
            if (UserManager.SupportsUserRole)
            {
                var roles = await UserManager.GetRolesAsync(user, cancellationToken);
                foreach (var roleName in roles)
                {
                    id.AddClaim(new Claim(UserManager.Options.ClaimType.Role, roleName, ClaimValueTypes.String));
                    if (RoleManager.SupportsRoleClaims)
                    {
                        var role = await RoleManager.FindByNameAsync(roleName);
                        if (role != null)
                        {
                            id.AddClaims(await RoleManager.GetClaimsAsync(role, cancellationToken));
                        }
                    }
                }
            }
            if (UserManager.SupportsUserClaim)
            {
                id.AddClaims(await UserManager.GetClaimsAsync(user, cancellationToken));
            }
            return id;
        }
    }
}