// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Configures response compression behavior for HTTPS on a per-request basis.
/// </summary>
public interface IHttpsCompressionFeature
{
    /// <summary>
    /// The <see cref="HttpsCompressionMode"/> to use.
    /// </summary>
    HttpsCompressionMode Mode { get; set; }
}
