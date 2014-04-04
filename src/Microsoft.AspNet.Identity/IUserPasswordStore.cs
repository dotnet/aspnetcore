using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's password hash
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserPasswordStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Set the user password hash
        /// </summary>
        /// <param name="user"></param>
        /// <param name="passwordHash"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetPasswordHash(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Get the user password hash
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetPasswordHash(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns true if a user has a password set
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> HasPassword(TUser user, CancellationToken cancellationToken = default(CancellationToken));
    }
}