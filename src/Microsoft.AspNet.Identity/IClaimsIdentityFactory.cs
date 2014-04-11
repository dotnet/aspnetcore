using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface for creating a ClaimsIdentity from an user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IClaimsIdentityFactory<TUser>
        where TUser : class
    {
        /// <summary>
        ///     Create a ClaimsIdentity from an user using a UserManager
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ClaimsIdentity> CreateAsync(UserManager<TUser> manager, TUser user, string authenticationType, CancellationToken cancellationToken = default(CancellationToken));
    }
}