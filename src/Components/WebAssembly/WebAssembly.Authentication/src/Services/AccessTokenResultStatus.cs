// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents the possible results from trying to acquire an access token.
    /// </summary>
    public enum AccessTokenResultStatus
    {
        /// <summary>
        /// The token was successfully acquired.
        /// </summary>
        Success,

        /// <summary>
        /// A redirect is needed in order to provision the token.
        /// </summary>
        RequiresRedirect,
    }
}
