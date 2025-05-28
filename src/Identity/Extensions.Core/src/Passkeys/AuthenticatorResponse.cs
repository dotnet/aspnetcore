// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the base class for responses returned by an authenticator during credential creation or retrieval
/// operations.
/// </summary>
internal abstract class AuthenticatorResponse(BufferSource clientDataJSON)
{
    /// <summary>
    /// Gets or sets the client data passed to
    /// <c>navigator.credentials.create()</c> or <c>navigator.credentials.get()</c>.
    /// </summary>
    public BufferSource ClientDataJSON { get; } = clientDataJSON;
}
