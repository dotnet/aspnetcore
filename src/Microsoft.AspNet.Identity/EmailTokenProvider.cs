using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     TokenProvider that generates tokens from the user's security stamp and notifies a user via their email
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class EmailTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser>
        where TUser : class
    {
        private string _body;
        private string _subject;

        /// <summary>
        ///     Email subject used when a token notification is received
        /// </summary>
        public string Subject
        {
            get { return _subject ?? string.Empty; }
            set { _subject = value; }
        }

        /// <summary>
        ///     Format string which will be used for the email body, it will be passed the token for the first parameter
        /// </summary>
        public string BodyFormat
        {
            get { return _body ?? "{0}"; }
            set { _body = value; }
        }

        /// <summary>
        ///     True if the user has an email set
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<bool> IsValidProviderForUserAsync(UserManager<TUser> manager, TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var email = await manager.GetEmailAsync(user, cancellationToken);
            return !String.IsNullOrWhiteSpace(email) && await manager.IsEmailConfirmedAsync(user, cancellationToken);
        }

        /// <summary>
        ///     Returns the email of the user for entropy in the token
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager,
            TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            var email = await manager.GetEmailAsync(user, cancellationToken);
            return "Email:" + purpose + ":" + email;
        }

        /// <summary>
        ///     Notifies the user with a token via email using the Subject and BodyFormat
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
            return manager.SendEmailAsync(user, Subject, String.Format(CultureInfo.CurrentCulture, BodyFormat, token), cancellationToken);
        }
    }
}