// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Features.Authentication;

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
