// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Contains the options used by the <see cref="AuthenticationHandler{T}"/>.
    /// </summary>
    public class AuthenticationSchemeOptions
    {
        /// <summary>
        /// Check that the options are valid. Should throw an exception if things are not ok.
        /// </summary>
        public virtual void Validate()
        {
        }

        /// <summary>
        /// Gets or sets the issuer that should be used for any claims that are created
        /// </summary>
        public string ClaimsIssuer { get; set; }

        /// <summary>
        /// Instance used for events
        /// </summary>
        public object Events { get; set; }

        /// <summary>
        /// If set, will be used as the service type to get the Events instance instead of the property.
        /// </summary>
        public Type EventsType { get; set; }
    }
}
