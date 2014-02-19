#if NET45

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores user specific claims
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IUserClaimStore<TUser, in TKey> : IUserStore<TUser, TKey> where TUser : class, IUser<TKey>
    {
        /// <summary>
        ///     Returns the claims for the user with the issuer set
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<IList<Claim>> GetClaims(TUser user);

        /// <summary>
        ///     Add a new user claim
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claim"></param>
        /// <returns></returns>
        Task AddClaim(TUser user, Claim claim);

        /// <summary>
        ///     Remove a user claim
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claim"></param>
        /// <returns></returns>
        Task RemoveClaim(TUser user, Claim claim);
    }
}

#endif