// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Options for configuring the client.
/// </summary>
public class ClientOptions
{
    /// <summary>
    /// Gets or sets the base URL for the client.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the email confirmation route for the client.
    /// </summary>
    public string? EmailconfirmationRoute { get; set; }

    /// <summary>
    /// Gets or sets the developer-defined add-ons for the client.
    /// </summary>
    public IDictionary<string, object>? DeveloperDefinedAddOns { get; set; }
}
