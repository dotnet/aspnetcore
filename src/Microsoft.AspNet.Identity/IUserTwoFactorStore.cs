using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores whether two factor is enabled for a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IUserTwoFactorStore<TUser, in TKey> : IUserStore<TUser, TKey> where TUser : class, IUser<TKey>
    {
        /// <summary>
        ///     Sets whether two factor is enabled for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="enabled"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetTwoFactorEnabled(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns whether two factor is enabled for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> GetTwoFactorEnabled(TUser user, CancellationToken cancellationToken = default(CancellationToken));
    }
}