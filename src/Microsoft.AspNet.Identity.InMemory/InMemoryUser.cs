using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNet.Identity.InMemory
{
    public class InMemoryUser
    {
        private readonly IList<Claim> _claims;
        private readonly IList<UserLoginInfo> _logins;
        private readonly IList<string> _roles;

        public InMemoryUser()
        {
            Id = Guid.NewGuid().ToString();
            _logins = new List<UserLoginInfo>();
            _claims = new List<Claim>();
            _roles = new List<string>();
        }

        public InMemoryUser(string name) : this()
        {
            UserName = name;
        }

        /// <summary>
        ///     Email
        /// </summary>
        public virtual string Email { get; set; }

        /// <summary>
        ///     True if the email is confirmed, default is false
        /// </summary>
        public virtual bool EmailConfirmed { get; set; }

        /// <summary>
        ///     The salted/hashed form of the user password
        /// </summary>
        public virtual string PasswordHash { get; set; }

        /// <summary>
        ///     A random value that should change whenever a users credentials have changed (password changed, login removed)
        /// </summary>
        public virtual string SecurityStamp { get; set; }

        /// <summary>
        ///     PhoneNumber for the user
        /// </summary>
        public virtual string PhoneNumber { get; set; }

        /// <summary>
        ///     True if the phone number is confirmed, default is false
        /// </summary>
        public virtual bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        ///     Is two factor enabled for the user
        /// </summary>
        public virtual bool TwoFactorEnabled { get; set; }

        /// <summary>
        ///     DateTime in UTC when lockout ends, any time in the past is considered not locked out.
        /// </summary>
        public virtual DateTimeOffset LockoutEnd { get; set; }

        /// <summary>
        ///     Is lockout enabled for this user
        /// </summary>
        public virtual bool LockoutEnabled { get; set; }

        /// <summary>
        ///     Used to record failures for the purposes of lockout
        /// </summary>
        public virtual int AccessFailedCount { get; set; }

        public IList<UserLoginInfo> Logins
        {
            get { return _logins; }
        }

        public IList<Claim> Claims
        {
            get { return _claims; }
        }

        public IList<string> Roles
        {
            get { return _roles; }
        }

        public virtual string Id { get; set; }
        public virtual string UserName { get; set; }
    }
}