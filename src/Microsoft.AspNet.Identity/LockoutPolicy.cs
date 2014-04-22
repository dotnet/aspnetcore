using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Configuration for lockout
    /// </summary>
    public class LockoutPolicy
    {
        /// <summary>
        ///     If true, will enable user lockout when users are created
        /// </summary>
        public bool UserLockoutEnabledByDefault { get; set; }

        /// <summary>
        ///     Number of access attempts allowed for a user before lockout (if enabled)
        /// </summary>
        public int MaxFailedAccessAttemptsBeforeLockout { get; set; }

        /// <summary>
        ///     Default amount of time an user is locked out for after MaxFailedAccessAttempsBeforeLockout is reached
        /// </summary>
        public TimeSpan DefaultAccountLockoutTimeSpan { get; set; }
    }
}