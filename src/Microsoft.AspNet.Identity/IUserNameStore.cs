using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's email
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserNameStore<TUser, in TKey> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Set the user name
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetUserName(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Get the user name
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetUserName(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns the user associated with this name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TUser> FindByName(string name, CancellationToken cancellationToken = default(CancellationToken));
    }
}