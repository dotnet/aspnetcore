// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;
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
        public ClaimsIdentityFactory(UserManager<TUser> userManager, RoleManager<TRole> roleManager, 
            IOptionsAccessor<IdentityOptions> optionsAccessor)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException("userManager");
            }
            if (roleManager == null)
            {
                throw new ArgumentNullException("roleManager");
            }
            if (optionsAccessor == null || optionsAccessor.Options == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }
            UserManager = userManager;
            RoleManager = roleManager;
            Options = optionsAccessor.Options;
        }

        public UserManager<TUser> UserManager { get; private set; }
        public RoleManager<TRole> RoleManager { get; private set; }
        public IdentityOptions Options { get; private set; }

        /// <summary>
        ///     CreateAsync a ClaimsIdentity from a user
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<ClaimsIdentity> CreateAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var userId = await UserManager.GetUserIdAsync(user, cancellationToken);
            var userName = await UserManager.GetUserNameAsync(user, cancellationToken);
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType, Options.ClaimsIdentity.UserNameClaimType,
                Options.ClaimsIdentity.RoleClaimType);
            id.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId));
            id.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, userName, ClaimValueTypes.String));
            if (UserManager.SupportsUserSecurityStamp)
            {
                id.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, 
                    await UserManager.GetSecurityStampAsync(user, cancellationToken)));
            }
            if (UserManager.SupportsUserRole)
            {
                var roles = await UserManager.GetRolesAsync(user, cancellationToken);
                foreach (var roleName in roles)
                {
                    id.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName, ClaimValueTypes.String));
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