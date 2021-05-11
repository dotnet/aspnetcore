// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    /// <summary>
    /// The HTTP authentication feature.
    /// </summary>
    public interface IHttpAuthenticationFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="ClaimsPrincipal"/> associated with the HTTP request.
        /// </summary>
        ClaimsPrincipal? User { get; set; }
    }
}
