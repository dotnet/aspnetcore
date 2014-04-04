using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that maps users to login providers, i.e. Google, Facebook, Twitter, Microsoft
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserLoginStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Adds a user login with the specified provider and key
        /// </summary>
        /// <param name="user"></param>
        /// <param name="login"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddLogin(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Removes the user login with the specified combination if it exists, returns true if found and removed
        /// </summary>
        /// <param name="user"></param>
        /// <param name="login"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RemoveLogin(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns the linked accounts for this user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<UserLoginInfo>> GetLogins(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns the user associated with this login
        /// </summary>
        /// <param name="login"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TUser> Find(UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken));
    }
}