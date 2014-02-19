using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes basic user management apis
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IUserStore<TUser, in TKey> : IDisposable where TUser : class, IUser<TKey>
    {
        /// <summary>
        ///     Insert a new user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task Create(TUser user);

        /// <summary>
        ///     Update a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task Update(TUser user);

        /// <summary>
        ///     Delete a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task Delete(TUser user);

        /// <summary>
        ///     Finds a user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<TUser> FindById(TKey userId);

        /// <summary>
        ///     Find a user by name
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task<TUser> FindByName(string userName);
    }
}