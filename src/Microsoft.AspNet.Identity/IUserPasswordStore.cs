using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's password hash
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IUserPasswordStore<TUser, in TKey> : IUserStore<TUser, TKey> where TUser : class, IUser<TKey>
    {
        /// <summary>
        ///     Set the user password hash
        /// </summary>
        /// <param name="user"></param>
        /// <param name="passwordHash"></param>
        /// <returns></returns>
        Task SetPasswordHash(TUser user, string passwordHash);

        /// <summary>
        ///     Get the user password hash
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<string> GetPasswordHash(TUser user);

        /// <summary>
        ///     Returns true if a user has a password set
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> HasPassword(TUser user);
    }
}