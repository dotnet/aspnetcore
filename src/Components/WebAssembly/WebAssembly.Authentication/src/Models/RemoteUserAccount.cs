// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// A user account.
    /// </summary>
    /// <remarks>
    /// The information in this type will be use to produce a <see cref="System.Security.Claims.ClaimsPrincipal"/> for the application.
    /// </remarks>
    public class RemoteUserAccount
    {
        /// <summary>
        /// Gets or sets properties not explicitly mapped about the user.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; set; }
    }
}
