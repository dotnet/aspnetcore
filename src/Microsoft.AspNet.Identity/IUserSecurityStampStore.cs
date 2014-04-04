using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's security stamp
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserSecurityStampStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Set the security stamp for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="stamp"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetSecurityStamp(TUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Get the user security stamp
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetSecurityStamp(TUser user, CancellationToken cancellationToken = default(CancellationToken));
    }
}