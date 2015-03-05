using System;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    public class EmailTokenProviderOptions
    {
        public string Name { get; set; } = "Email";
    }

    /// <summary>
    ///     TokenProvider that generates tokens from the user's security stamp and notifies a user via their email
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class EmailTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser>
        where TUser : class
    {
        public EmailTokenProvider(IOptions<EmailTokenProviderOptions> options, string name = "")
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = options.GetNamedOptions(name);
        }

        public EmailTokenProviderOptions Options { get; private set; }

        public override string Name { get { return Options.Name; } }

        /// <summary>
        ///     True if the user has an email set
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            var email = await manager.GetEmailAsync(user);
            return !string.IsNullOrWhiteSpace(email) && await manager.IsEmailConfirmedAsync(user);
        }

        /// <summary>
        ///     Returns the email of the user for entropy in the token
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager,
            TUser user)
        {
            var email = await manager.GetEmailAsync(user);
            return "Email:" + purpose + ":" + email;
        }
    }
}