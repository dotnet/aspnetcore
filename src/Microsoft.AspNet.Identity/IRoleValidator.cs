using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate a role
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRoleValidator<TRole, TKey>
        where TRole : class, IRole<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     Validate the user
        /// </summary>
        /// <param name="role"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        Task<IdentityResult> Validate(RoleManager<TRole, TKey> manager, TRole role);
    }
}