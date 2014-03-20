using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Validates roles before they are saved
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class RoleValidator<TRole, TKey> : IRoleValidator<TRole, TKey>
        where TRole : class, IRole<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     Validates a role before saving
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> Validate(RoleManager<TRole, TKey> manager, TRole role)
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

        private static async Task ValidateRoleName(RoleManager<TRole, TKey> manager, TRole role,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.PropertyTooShort, "Name"));
            }
            else
            {
                var owner = await manager.FindByName(role.Name);
                if (owner != null && !EqualityComparer<TKey>.Default.Equals(owner.Id, role.Id))
                {
                    errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.DuplicateName, role.Name));
                }
            }
        }
    }
}