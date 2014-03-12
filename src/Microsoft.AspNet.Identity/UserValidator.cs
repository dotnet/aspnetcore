using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if NET45
using System.Net.Mail;
#endif
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Validates users before they are saved
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class UserValidator<TUser, TKey> : IUserValidator<TUser, TKey>
        where TUser : class, IUser<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public UserValidator()
        {
            AllowOnlyAlphanumericUserNames = true;
        }

        /// <summary>
        ///     Only allow [A-Za-z0-9@_] in UserNames
        /// </summary>
        public bool AllowOnlyAlphanumericUserNames { get; set; }

        /// <summary>
        ///     If set, enforces that emails are non empty, valid, and unique
        /// </summary>
        public bool RequireUniqueEmail { get; set; }

        /// <summary>
        ///     Validates a user before saving
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> Validate(UserManager<TUser, TKey> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var errors = new List<string>();
            await ValidateUserName(manager, user, errors);
            if (RequireUniqueEmail)
            {
                await ValidateEmail(manager, user, errors);
            }
            return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }
        
        // TODO: Revisit extensibility for Validators

        /// <summary>
        ///     Returns true if the character is a digit between '0' and '9'
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public virtual bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        ///     Returns true if the character is between 'a' and 'z'
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public virtual bool IsLower(char c)
        {
            return c >= 'a' && c <= 'z';
        }

        /// <summary>
        ///     Returns true if the character is between 'A' and 'Z'
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public virtual bool IsUpper(char c)
        {
            return c >= 'A' && c <= 'Z';
        }

        /// <summary>
        ///     Returns true if the character is upper, lower, or a digit
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public virtual bool IsAlphaNumeric(char c)
        {
            return IsUpper(c) || IsLower(c) || IsDigit(c) || c == '@' || c == '_';
        }

        private async Task ValidateUserName(UserManager<TUser, TKey> manager, TUser user, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.PropertyTooShort, "Name"));
            }
            else if (AllowOnlyAlphanumericUserNames && !user.UserName.All(IsAlphaNumeric))
            {
                // If any characters are not letters or digits, its an illegal user name
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.InvalidUserName, user.UserName));
            }
            else
            {
                var owner = await manager.FindByName(user.UserName);
                if (owner != null && !EqualityComparer<TKey>.Default.Equals(owner.Id, user.Id))
                {
                    errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.DuplicateName, user.UserName));
                }
            }
        }

        // make sure email is not empty, valid, and unique
        private async Task ValidateEmail(UserManager<TUser, TKey> manager, TUser user, List<string> errors)
        {
            var email = await manager.GetEmailStore().GetEmail(user).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.PropertyTooShort, "Email"));
                return;
            }
#if NET45
            try
            {
                var m = new MailAddress(email);
            }
            catch (FormatException)
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.InvalidEmail, email));
                return;
            }
#endif
            var owner = await manager.FindByEmail(email);
            if (owner != null && !EqualityComparer<TKey>.Default.Equals(owner.Id, user.Id))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, Resources.DuplicateEmail, email));
            }
        }
    }
}