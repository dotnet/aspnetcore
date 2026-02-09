// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Options for configuring query string redaction in HTTP telemetry.
/// </summary>
public sealed class UrlQueryRedactionOptions
{
    /// <summary>
    /// Initializes a new instance of <see cref="UrlQueryRedactionOptions"/>.
    /// </summary>
    public UrlQueryRedactionOptions()
    {
    }

    /// <summary>
    /// Gets the set of query parameter names whose values should be redacted.
    /// Parameter name matching is case-insensitive.
    /// </summary>
    /// <remarks>
    /// Default sensitive parameters include: password, token, api_key, apikey, secret,
    /// access_token, refresh_token, credential, key, sig, signature.
    /// </remarks>
    public HashSet<string> SensitiveQueryParameters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "token",
        "api_key",
        "apikey",
        "secret",
        "access_token",
        "refresh_token",
        "credential",
        "key",
        "sig",
        "signature"
    };

    /// <summary>
    /// Gets or sets the placeholder text used to replace redacted values.
    /// </summary>
    /// <value>Defaults to "[Redacted]".</value>
    public string RedactedPlaceholder { get; set; } = "[Redacted]";
}
