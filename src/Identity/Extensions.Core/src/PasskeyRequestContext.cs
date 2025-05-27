// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Contains passkey-relevant information about the current request.
/// </summary>
public sealed class PasskeyRequestContext
{
    /// <summary>
    /// Gets or sets the server domain.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the request origin.
    /// </summary>
    public string? Origin { get; set; }
}
