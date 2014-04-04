using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate a role
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRoleValidator<TRole> where TRole : class
    {
        /// <summary>
        ///     Validate the user
        /// </summary>
        /// <param name="role"></param>
        /// <param name="manager"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IdentityResult> Validate(RoleManager<TRole> manager, TRole role, CancellationToken cancellationToken = default(CancellationToken));
    }
}