using System;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Options for the <see cref="EmailTokenProvider{TUser}"/> class.
    /// </summary>
    public class EmailTokenProviderOptions
    {
        /// <summary>
        /// Gets or sets the unique name used for an instance of <see cref="EmailTokenProvider{TUser}"/>.
        /// </summary>
        /// <value>
        /// The unique name used for an instance of <see cref="EmailTokenProvider{TUser}"/>.
        /// </value>
        public string Name { get; set; } = "Email";
    }

    /// <summary>
    /// TokenProvider that generates tokens from the user's security stamp and notifies a user via email.
    /// </summary>
    /// <typeparam name="TUser">The type used to represent a user.</typeparam>
    public class EmailTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser>
        where TUser : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailTokenProvider{TUser}"/> class.
        /// </summary>
        /// <param name="options">The configured <see cref="DataProtectionTokenProviderOptions"/>.</param>
        /// <param name="name">The unique name for this instance of <see cref="EmailTokenProvider{TUser}"/>.</param>
        public EmailTokenProvider(IOptions<EmailTokenProviderOptions> options, string name = "")
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = options.GetNamedOptions(name);
        }

        /// <summary>
        /// Gets the options for this instance of <see cref="EmailTokenProvider{TUser}"/>.
        /// </summary>
        /// <value>
        /// The options for this instance of <see cref="EmailTokenProvider{TUser}"/>.
        /// </value>
        public EmailTokenProviderOptions Options { get; private set; }

        /// <summary>
        /// Gets the unique name for this instance of <see cref="EmailTokenProvider{TUser}"/>.
        /// </summary>
        /// <value>
        /// The unique name for this instance of <see cref="EmailTokenProvider{TUser}"/>.
        /// </value>
        public override string Name { get { return Options.Name; } }

        /// <summary>
        /// Checks if a two factor authentication token can be generated for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve the <paramref name="user"/> from.</param>
        /// <param name="user">The <see cref="TUser"/> to check for the possibility of generating a two factor authentication token.</param>
        /// <returns>True if the user has an email address set, otherwise false.</returns>
        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            var email = await manager.GetEmailAsync(user);
            return !string.IsNullOrWhiteSpace(email) && await manager.IsEmailConfirmedAsync(user);
        }

        /// <summary>
        /// Returns the a value for the user used as entropy in the generated token.
        /// </summary>
        /// <param name="purpose">The purpose of the two factor authentication token.</param>
        /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve the <paramref name="user"/> from.</param>
        /// <param name="user">The <see cref="TUser"/> to check for the possibility of generating a two factor authentication token.</param>
        /// <returns>A string suitable for use as entropy in token generation.</returns>
        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager,
            TUser user)
        {
            var email = await manager.GetEmailAsync(user);
            return "Email:" + purpose + ":" + email;
        }
    }
}