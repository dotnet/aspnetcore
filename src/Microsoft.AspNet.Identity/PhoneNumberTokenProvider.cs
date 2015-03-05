using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    public class PhoneNumberTokenProviderOptions
    {
        public string Name { get; set; } = "Phone";
    }

    /// <summary>
    ///     TokenProvider that generates tokens from the user's security stamp and notifies a user via their phone number
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class PhoneNumberTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser>
        where TUser : class
    {
        public PhoneNumberTokenProvider(IOptions<PhoneNumberTokenProviderOptions> options)
        {
            if (options == null || options.Options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = options.Options;
        }

        public PhoneNumberTokenProviderOptions Options { get; private set; }

        public override string Name { get { return Options.Name; } }

        /// <summary>
        ///     Returns true if the user has a phone number set
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            var phoneNumber = await manager.GetPhoneNumberAsync(user);
            return !string.IsNullOrWhiteSpace(phoneNumber) && await manager.IsPhoneNumberConfirmedAsync(user);
        }

        /// <summary>
        ///     Returns the phone number of the user for entropy in the token
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            var phoneNumber = await manager.GetPhoneNumberAsync(user);
            return "PhoneNumber:" + purpose + ":" + phoneNumber;
        }
    }
}