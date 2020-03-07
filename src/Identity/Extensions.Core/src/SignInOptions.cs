// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Options for configuring sign in.
    /// </summary>
    public class SignInOptions
    {
        /// <summary>
        /// Gets or sets a flag indicating whether a confirmed email address is required to sign in. Defaults to false.
        /// </summary>
        /// <value>True if a user must have a confirmed email address before they can sign in, otherwise false.</value>
        public bool RequireConfirmedEmail { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether a confirmed telephone number is required to sign in. Defaults to false.
        /// </summary>
        /// <value>True if a user must have a confirmed telephone number before they can sign in, otherwise false.</value>
        public bool RequireConfirmedPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether a confirmed <see cref="IUserConfirmation{TUser}"/> account is required to sign in. Defaults to false.
        /// </summary>
        /// <value>True if a user must have a confirmed account before they can sign in, otherwise false.</value>
        public bool RequireConfirmedAccount { get; set; }
    }
}
