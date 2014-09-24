using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    public class PhoneNumberTokenProviderOptions
    {
        public string Name { get; set; } = Resources.DefaultPhoneNumberTokenProviderName;

        /// <summary>
        ///     Message contents which should contain a format string which the token will be the only argument
        /// </summary>
        public string MessageFormat { get; set; } = "Your security code is: {0}";
    }

    /// <summary>
    ///     TokenProvider that generates tokens from the user's security stamp and notifies a user via their phone number
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class PhoneNumberTokenProvider<TUser>(PhoneNumberTokenProviderOptions options) : TotpSecurityStampBasedTokenProvider<TUser>
        where TUser : class
    {
        public PhoneNumberTokenProvider() : this(new PhoneNumberTokenProviderOptions()) { }

        public PhoneNumberTokenProviderOptions Options { get; } = options;

        public override string Name { get { return Options.Name; } }

        /// <summary>
        ///     Returns true if the user has a phone number set
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            var phoneNumber = await manager.GetPhoneNumberAsync(user, cancellationToken);
            return !String.IsNullOrWhiteSpace(phoneNumber) && await manager.IsPhoneNumberConfirmedAsync(user, cancellationToken);
        }

        /// <summary>
        ///     Returns the phone number of the user for entropy in the token
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager,
            TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            var phoneNumber = await manager.GetPhoneNumberAsync(user, cancellationToken);
            return "PhoneNumber:" + purpose + ":" + phoneNumber;
        }

        /// <summary>
        ///     Notifies the user with a token via SMS using the MessageFormat
        /// </summary>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override Task NotifyAsync(string token, UserManager<TUser> manager, TUser user, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            return manager.SendSmsAsync(user, String.Format(CultureInfo.CurrentCulture, Options.MessageFormat, token), cancellationToken);
        }
    }
}