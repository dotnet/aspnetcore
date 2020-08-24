// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents an access token for a given user and scopes.
    /// </summary>
    public class AccessToken
    {
        /// <summary>
        /// Gets or sets the list of granted scopes for the token.
        /// </summary>
        public IReadOnlyList<string> GrantedScopes { get; set; }

        /// <summary>
        /// Gets the expiration time of the token.
        /// </summary>
        public DateTimeOffset Expires { get; set; }

        /// <summary>
        /// Gets the serialized representation of the token.
        /// </summary>
        public string Value { get; set; }
    }
}
