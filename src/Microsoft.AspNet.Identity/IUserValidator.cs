using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserValidator<TUser> where TUser : class
    {
        /// <summary>
        ///     Validate the user
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IdentityResult> Validate(UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken));
    }
}