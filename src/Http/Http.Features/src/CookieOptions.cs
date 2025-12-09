// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Options used to create a new cookie.
/// </summary>
/// <remarks>
/// A <see cref="CookieOptions"/> instance is intended to govern the behavior of an individual cookie.
/// Reusing the same <see cref="CookieOptions"/> instance across multiple cookies can lead to unintended
/// consequences, such as modifications affecting multiple cookies. We recommend instantiating a new
/// <see cref="CookieOptions"/> object for each cookie to ensure that the configuration is applied
/// independently.
/// </remarks>
public class CookieOptions
{
    private List<string>? _extensions;

    /// <summary>
    /// Creates a default cookie with a path of '/'.
    /// </summary>
    public CookieOptions()
    {
        Path = "/";
    }

    /// <summary>
    /// Creates a copy of the given <see cref="CookieOptions"/>.
    /// </summary>
    public CookieOptions(CookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Domain = options.Domain;
        Path = options.Path;
        Expires = options.Expires;
        Secure = options.Secure;
        SameSite = options.SameSite;
        HttpOnly = options.HttpOnly;
        MaxAge = options.MaxAge;
        IsEssential = options.IsEssential;

        if (options._extensions?.Count > 0)
        {
            _extensions = new List<string>(options._extensions);
        }
    }

    /// <summary>
    /// Gets or sets the domain to associate the cookie with.
    /// </summary>
    /// <returns>The domain to associate the cookie with.</returns>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the cookie path.
    /// </summary>
    /// <returns>The cookie path.</returns>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the expiration date and time for the cookie.
    /// </summary>
    /// <returns>The expiration date and time for the cookie.</returns>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether to transmit the cookie using Secure Sockets Layer (SSL)--that is, over HTTPS only.
    /// </summary>
    /// <returns>true to transmit the cookie only over an SSL connection (HTTPS); otherwise, false.</returns>
    public bool Secure { get; set; }

    /// <summary>
    /// Gets or sets the value for the SameSite attribute of the cookie. The default value is <see cref="SameSiteMode.Unspecified"/>
    /// </summary>
    /// <returns>The <see cref="SameSiteMode"/> representing the enforcement mode of the cookie.</returns>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Unspecified;

    /// <summary>
    /// Gets or sets a value that indicates whether a cookie is inaccessible by client-side script.
    /// </summary>
    /// <returns>true if a cookie must not be accessible by client-side script; otherwise, false.</returns>
    public bool HttpOnly { get; set; }

    /// <summary>
    /// Gets or sets the max-age for the cookie.
    /// </summary>
    /// <returns>The max-age date and time for the cookie.</returns>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Indicates if this cookie is essential for the application to function correctly. If true then
    /// consent policy checks may be bypassed. The default value is false.
    /// </summary>
    public bool IsEssential { get; set; }

    /// <summary>
    /// Gets a collection of additional values to append to the cookie.
    /// </summary>
    public IList<string> Extensions
    {
        get => _extensions ??= new List<string>();
    }

    /// <summary>
    /// Creates a <see cref="SetCookieHeaderValue"/> using the current options.
    /// </summary>
    public SetCookieHeaderValue CreateCookieHeader(string name, string value)
    {
        var cookie = new SetCookieHeaderValue(name, value)
        {
            Domain = Domain,
            Path = Path,
            Expires = Expires,
            Secure = Secure,
            HttpOnly = HttpOnly,
            MaxAge = MaxAge,
            SameSite = (Net.Http.Headers.SameSiteMode)SameSite,
        };

        if (_extensions?.Count > 0)
        {
            foreach (var extension in _extensions)
            {
                cookie.Extensions.Add(extension);
            }
        }

        return cookie;
    }
}
