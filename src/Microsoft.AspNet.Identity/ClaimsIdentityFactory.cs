using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Creates a ClaimsIdentity from a User
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class ClaimsIdentityFactory<TUser, TKey> : IClaimsIdentityFactory<TUser, TKey>
        where TUser : class, IUser<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     ClaimType used for the security stamp by default
        /// </summary>
        public const string DefaultSecurityStampClaimType = "AspNet.Identity.SecurityStamp";

        /// <summary>
        ///     Constructor
        /// </summary>
        public ClaimsIdentityFactory()
        {
            RoleClaimType = ClaimsIdentity.DefaultRoleClaimType;
            UserIdClaimType = ClaimTypes.NameIdentifier;
            UserNameClaimType = ClaimsIdentity.DefaultNameClaimType;
            SecurityStampClaimType = DefaultSecurityStampClaimType;
        }

        /// <summary>
        ///     Claim type used for role claims
        /// </summary>
        public string RoleClaimType { get; set; }

        /// <summary>
        ///     Claim type used for the user name
        /// </summary>
        public string UserNameClaimType { get; set; }

        /// <summary>
        ///     Claim type used for the user id
        /// </summary>
        public string UserIdClaimType { get; set; }

        /// <summary>
        ///     Claim type used for the user security stamp
        /// </summary>
        public string SecurityStampClaimType { get; set; }

        /// <summary>
        ///     Create a ClaimsIdentity from a user
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <returns></returns>
        public virtual async Task<ClaimsIdentity> Create(UserManager<TUser, TKey> manager, TUser user,
            string authenticationType)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var id = new ClaimsIdentity(authenticationType, UserNameClaimType, RoleClaimType);
            id.AddClaim(new Claim(UserIdClaimType, ConvertIdToString(user.Id), ClaimValueTypes.String));
            id.AddClaim(new Claim(UserNameClaimType, user.UserName, ClaimValueTypes.String));
            if (manager.SupportsUserSecurityStamp)
            {
                id.AddClaim(new Claim(SecurityStampClaimType, await manager.GetSecurityStamp(user.Id)));
            }
            if (manager.SupportsUserRole)
            {
                var roles = await manager.GetRoles(user.Id);
                foreach (var roleName in roles)
                {
                    id.AddClaim(new Claim(RoleClaimType, roleName, ClaimValueTypes.String));
                }
            }
            if (manager.SupportsUserClaim)
            {
                id.AddClaims(await manager.GetClaims(user.Id));
            }
            return id;
        }

        /// <summary>
        ///     Convert the key to a string, by default just calls .ToString()
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual string ConvertIdToString(TKey key)
        {
            if (key == null || key.Equals(default(TKey)))
            {
                return null;
            }
            return key.ToString();
        }
    }
}