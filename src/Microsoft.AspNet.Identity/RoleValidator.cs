// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Validates roles before they are saved
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public class RoleValidator<TRole> : IRoleValidator<TRole> where TRole : class
    {
        public RoleValidator(IdentityErrorDescriber errors = null)
        {
            Describer = errors ?? new IdentityErrorDescriber();
        }

        private IdentityErrorDescriber Describer { get; set; }

        /// <summary>
        ///     Validates a role before saving
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            var errors = new List<IdentityError>();
            await ValidateRoleName(manager, role, errors);
            if (errors.Count > 0)
            {
                return IdentityResult.Failed(errors.ToArray());
            }
            return IdentityResult.Success;
        }

        private async Task ValidateRoleName(RoleManager<TRole> manager, TRole role,
            ICollection<IdentityError> errors)
        {
            var roleName = await manager.GetRoleNameAsync(role);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                errors.Add(Describer.InvalidRoleName(roleName));
            }
            else
            {
                var owner = await manager.FindByNameAsync(roleName);
                if (owner != null && 
                    !string.Equals(await manager.GetRoleIdAsync(owner), await manager.GetRoleIdAsync(role)))
                {
                    errors.Add(Describer.DuplicateRoleName(roleName));
                }
            }
        }
    }
}