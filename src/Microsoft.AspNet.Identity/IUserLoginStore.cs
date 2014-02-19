using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that maps users to login providers, i.e. Google, Facebook, Twitter, Microsoft
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IUserLoginStore<TUser, in TKey> : IUserStore<TUser, TKey> where TUser : class, IUser<TKey>
    {
        /// <summary>
        ///     Adds a user login with the specified provider and key
        /// </summary>
        /// <param name="user"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        Task AddLogin(TUser user, UserLoginInfo login);

        /// <summary>
        ///     Removes the user login with the specified combination if it exists, returns true if found and removed
        /// </summary>
        /// <param name="user"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        Task RemoveLogin(TUser user, UserLoginInfo login);

        /// <summary>
        ///     Returns the linked accounts for this user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<IList<UserLoginInfo>> GetLogins(TUser user);

        /// <summary>
        ///     Returns the user associated with this login
        /// </summary>
        /// <returns></returns>
        Task<TUser> Find(UserLoginInfo login);
    }
}