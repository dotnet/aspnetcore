using System;
#if NET45
using System.Security.Claims;
#else
using System.Security.ClaimsK;
#endif
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface for creating a ClaimsIdentity from an IUser
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IClaimsIdentityFactory<TUser, TKey>
        where TUser : class, IUser<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     Create a ClaimsIdentity from an user using a UserManager
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <returns></returns>
        Task<ClaimsIdentity> Create(UserManager<TUser, TKey> manager, TUser user, string authenticationType);
    }
}