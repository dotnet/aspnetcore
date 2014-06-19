// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Creates a ClaimsIdentity from a User
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class ClaimsIdentityFactory<TUser> : IClaimsIdentityFactory<TUser>
        where TUser : class
    {
        /// <summary>
        ///     CreateAsync a ClaimsIdentity from a user
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<ClaimsIdentity> CreateAsync(UserManager<TUser> manager, TUser user,
            string authenticationType, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var userId = await manager.GetUserIdAsync(user, cancellationToken);
            var userName = await manager.GetUserNameAsync(user, cancellationToken);
            var id = new ClaimsIdentity(authenticationType, manager.Options.ClaimType.UserName, manager.Options.ClaimType.Role);
            id.AddClaim(new Claim(manager.Options.ClaimType.UserId, userId));
            id.AddClaim(new Claim(manager.Options.ClaimType.UserName, userName, ClaimValueTypes.String));
            if (manager.SupportsUserSecurityStamp)
            {
                id.AddClaim(new Claim(manager.Options.ClaimType.SecurityStamp, await manager.GetSecurityStampAsync(user, cancellationToken)));
            }
            if (manager.SupportsUserRole)
            {
                var roles = await manager.GetRolesAsync(user, cancellationToken);
                foreach (var roleName in roles)
                {
                    id.AddClaim(new Claim(manager.Options.ClaimType.Role, roleName, ClaimValueTypes.String));
                }
            }
            if (manager.SupportsUserClaim)
            {
                id.AddClaims(await manager.GetClaimsAsync(user, cancellationToken));
            }
            return id;
        }
    }
}