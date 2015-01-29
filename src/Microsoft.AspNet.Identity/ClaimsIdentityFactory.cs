// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Provides methods to create a claims identity for a given user.
    /// </summary>
    /// <typeparam name="TUser">The type used to represent a user.</typeparam>
    /// <typeparam name="TRole">The type used to represent a role.</typeparam>
    public class ClaimsIdentityFactory<TUser, TRole> : IClaimsIdentityFactory<TUser>
        where TUser : class
        where TRole : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimsIdentityFactory"/> class.
        /// </summary>
        /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve user information from.</param>
        /// <param name="roleManager">The <see cref="RoleManager{TRole}"/> to retrieve a user's roles from.</param>
        /// <param name="optionsAccessor">The configured <see cref="IdentityOptions"/>.</param>
        public ClaimsIdentityFactory(
            UserManager<TUser> userManager, 
            RoleManager<TRole> roleManager, 
            IOptions<IdentityOptions> optionsAccessor)
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

        /// <summary>
        /// Gets the <see cref="UserManager{TUser}"/> for this factory.
        /// </summary>
        /// <value>
        /// The current <see cref="UserManager{TUser}"/> for this factory instance.
        /// </value>
        public UserManager<TUser> UserManager { get; private set; }

        /// <summary>
        /// Gets the <see cref="RoleManager{TRole}"/> for this factory.
        /// </summary>
        /// <value>
        /// The current <see cref="RoleManager{TRole}"/> for this factory instance.
        /// </value>
        public RoleManager<TRole> RoleManager { get; private set; }

        /// <summary>
        /// Gets the <see cref="IdentityOptions"/> for this factory.
        /// </summary>
        /// <value>
        /// The current <see cref="IdentityOptions"/> for this factory instance.
        /// </value>
        public IdentityOptions Options { get; private set; }

        /// <summary>
        /// Creates a populated <see cref="ClaimsIdentity"/> for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user instance to create claims on.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the tasks to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the started task.</returns>
        public virtual async Task<ClaimsIdentity> CreateAsync(
            TUser user,
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