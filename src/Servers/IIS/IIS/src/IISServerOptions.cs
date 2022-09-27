// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides configuration for IIS In-Process.
/// </summary>
public class IISServerOptions
{
    /// <summary>
    /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="HttpContext.Request"/> and <see cref="HttpContext.Response"/>
    /// </summary>
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    public bool AllowSynchronousIO { get; set; }

    /// <summary>
    /// If true the server should set HttpContext.User. If false the server will only provide an
    /// identity when explicitly requested by the AuthenticationScheme.
    /// Note Windows Authentication must also be enabled in IIS for this to work.
    /// </summary>
    public bool AutomaticAuthentication { get; set; } = true;

    /// <summary>
    /// Sets the display name shown to users on login pages. The default is null.
    /// </summary>
    public string? AuthenticationDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the maximum unconsumed incoming bytes the server will buffer for incoming request body.
    /// </summary>
    /// <value>
    /// Defaults to 1 MB.
    /// </value>
    public int MaxRequestBodyBufferSize { get; set; } = 1024 * 1024; // Matches kestrel (sorta)

    /// <summary>
    /// Used to indicate if the authentication handler should be registered. This is only done if ANCM indicates
    /// IIS has a non-anonymous authentication enabled, or for back compat with ANCMs that did not provide this information.
    /// </summary>
    internal bool ForwardWindowsAuthentication { get; set; } = true;

    internal string[] ServerAddresses { get; set; } = default!; // Set by configuration.

    // Matches the default maxAllowedContentLength in IIS (~28.6 MB)
    // https://www.iis.net/configreference/system.webserver/security/requestfiltering/requestlimits#005
    private long? _maxRequestBodySize = 30000000;

    internal long IisMaxRequestSizeLimit; // Used for verifying if limit set in managed exceeds native

    /// <summary>
    /// Gets or sets the maximum allowed size of any request body in bytes.
    /// When set to null, the maximum request length will not be restricted in ASP.NET Core.
    /// However, the IIS maxAllowedContentLength will still restrict content length requests (30,000,000 by default).
    /// This limit has no effect on upgraded connections which are always unlimited.
    /// This can be overridden per-request via <see cref="IHttpMaxRequestBodySizeFeature"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to 30,000,000 bytes (~28.6 MB).
    /// </remarks>
    public long? MaxRequestBodySize
    {
        get => _maxRequestBodySize;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.NonNegativeNumberOrNullRequired);
            }
            _maxRequestBodySize = value;
        }
    }
}
