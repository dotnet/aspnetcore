using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     TokenProvider that generates time based codes using the user's security stamp
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public abstract class TotpSecurityStampBasedTokenProvider<TUser> : IUserTokenProvider<TUser>
        where TUser : class
    {
        public abstract string Name { get; }

        /// <summary>
        ///     Generate a token for the user using their security stamp
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            var token = await manager.CreateSecurityTokenAsync(user);
            var modifier = await GetUserModifierAsync(purpose, manager, user);
            return Rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Validate the token for the user
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            int code;
            if (!int.TryParse(token, out code))
            {
                return false;
            }
            var securityToken = await manager.CreateSecurityTokenAsync(user);
            var modifier = await GetUserModifierAsync(purpose, manager, user);
            return securityToken != null && Rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
        }

        /// <summary>
        ///     Used for entropy in the token, uses the user.Id by default
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            var userId = await manager.GetUserIdAsync(user);
            return "Totp:" + purpose + ":" + userId;
        }

        public abstract Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
    }
}