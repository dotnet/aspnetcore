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
    public class RoleValidator<TRole, TKey> : IRoleValidator<TRole>
        where TRole : class, IRole<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="manager"></param>
        public RoleValidator(RoleManager<TRole, TKey> manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            Manager = manager;
        }

        private RoleManager<TRole, TKey> Manager { get; set; }

        /// <summary>
        ///     Validates a role before saving
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> Validate(TRole item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            var errors = new List<string>();
            await ValidateRoleName(item, errors);
            if (errors.Count > 0)
            {
                return IdentityResult.Failed(errors.ToArray());
            }
            return IdentityResult.Success;
        }

        private async Task ValidateRoleName(TRole role, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.PropertyTooShort, "Name"));
            }
            else
            {
                var owner = await Manager.FindByName(role.Name);
                if (owner != null && !EqualityComparer<TKey>.Default.Equals(owner.Id, role.Id))
                {
                    errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.DuplicateName, role.Name));
                }
            }
        }
    }
}