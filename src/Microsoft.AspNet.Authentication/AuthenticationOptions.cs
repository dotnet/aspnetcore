// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Base Options for all authentication middleware
    /// </summary>
    public abstract class AuthenticationOptions
    {
        private string _authenticationScheme;

        /// <summary>
        /// The AuthenticationScheme in the options corresponds to the logical name for a particular authentication scheme. A different
        /// value may be assigned in order to use the same authentication middleware type more than once in a pipeline.
        /// </summary>
        public string AuthenticationScheme
        {
            get { return _authenticationScheme; }
            set
            {
                _authenticationScheme = value;
                Description.AuthenticationScheme = value;
            }
        }

        /// <summary>
        /// If true the authentication middleware alter the request user coming in. If false the authentication middleware will only provide
        /// identity when explicitly indicated by the AuthenticationScheme.
        /// </summary>
        public bool AutomaticAuthenticate { get; set; }

        /// <summary>
        /// If true the authentication middleware should handle automatic challenge.
        /// If false the authentication middleware will only alter responses when explicitly indicated by the AuthenticationScheme.
        /// </summary>
        public bool AutomaticChallenge { get; set; }

        /// <summary>
        /// Gets or sets the issuer that should be used for any claims that are created
        /// </summary>
        public string ClaimsIssuer { get; set; }

        /// <summary>
        /// Additional information about the authentication type which is made available to the application.
        /// </summary>
        public AuthenticationDescription Description { get; set; } = new AuthenticationDescription();
    }
}
