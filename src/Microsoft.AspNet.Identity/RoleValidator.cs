// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>
        ///     Validates a role before saving
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            var errors = new List<string>();
            await ValidateRoleName(manager, role, errors);
            if (errors.Count > 0)
            {
                return IdentityResult.Failed(errors.ToArray());
            }
            return IdentityResult.Success;
        }

        private static async Task ValidateRoleName(RoleManager<TRole> manager, TRole role,
            ICollection<string> errors)
        {
            var roleName = await manager.GetRoleNameAsync(role);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.PropertyTooShort, "Name"));
            }
            else
            {
                var owner = await manager.FindByNameAsync(roleName);
                if (owner != null && !string.Equals(await manager.GetRoleIdAsync(owner), await manager.GetRoleIdAsync(role)))
                {
                    errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.DuplicateName, roleName));
                }
            }
        }
    }
}