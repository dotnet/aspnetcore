using System;
using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNet.ConfigurationModel;

namespace Microsoft.AspNet.Identity
{
    public class ClaimTypeOptions
    {
        /// <summary>
        ///     ClaimType used for the security stamp by default
        /// </summary>
        public static readonly string DefaultSecurityStampClaimType = "AspNet.Identity.SecurityStamp";

        public ClaimTypeOptions()
        {
            Role = ClaimTypes.Role;
            SecurityStamp = DefaultSecurityStampClaimType;
            UserId = ClaimTypes.NameIdentifier;
            UserName = ClaimTypes.Name;
        }

        public ClaimTypeOptions(IConfiguration config) : this()
        {
            IdentityOptions.Read(this, config);
        }

        /// <summary>
        ///     Claim type used for role claims
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        ///     Claim type used for the user name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///     Claim type used for the user id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     Claim type used for the user security stamp
        /// </summary>
        public string SecurityStamp { get; set; }

        public virtual void Copy(ClaimTypeOptions options)
        {
            if (options == null)
            {
                return;
            }
            Role = options.Role;
            SecurityStamp = options.SecurityStamp;
            UserId = options.UserId;
            UserName = options.UserName;
        }
    }
}