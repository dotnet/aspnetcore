// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Contains information about the state of the token binding protocol.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-tokenbinding"/>.
/// </remarks>
internal sealed class TokenBinding(string status)
{
    /// <summary>
    /// Gets the token binding status.
    /// </summary>
    /// <remarks>
    /// Supported values are "supported", "present", and "not-supported".
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-tokenbinding-status"/>.
    /// </remarks>
    public string Status { get; } = status;

    /// <summary>
    /// Gets or sets the token binding ID.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-tokenbinding-id"/>.
    /// </remarks>
    public string? Id { get; set; }
}
