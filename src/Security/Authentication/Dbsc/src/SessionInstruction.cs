// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// Represents the DBSC JSON session instructions returned to the browser during registration
/// and (optionally) refresh. Corresponds to the "JSON Session Instruction Format" defined in
/// W3C Device Bound Session Credentials §9.6.
/// </summary>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal sealed class SessionInstruction
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("session_identifier")]
    internal string SessionIdentifier { get; set; } = default!;

    /// <summary>
    /// Gets or sets the refresh URL.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("refresh_url")]
    internal string? RefreshUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session should continue to apply. Registration and
    /// refresh endpoints can set this to <see langword="false"/> to terminate the session. Defaults to
    /// <see langword="true"/>. See W3C Device Bound Session Credentials §9.6.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("continue")]
    internal bool Continue { get; set; } = true;

    /// <summary>
    /// Gets or sets the session scope.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("scope")]
    internal SessionScope? Scope { get; set; }

    /// <summary>
    /// Gets or sets the session credentials.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("credentials")]
    internal List<SessionCredential>? Credentials { get; set; }

    /// <summary>
    /// Gets or sets the list of out-of-scope hosts allowed to initiate DBSC refreshes. See W3C Device
    /// Bound Session Credentials §8.3 and §9.6.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("allowed_refresh_initiators")]
    internal List<string> AllowedRefreshInitiators { get; set; } = [];
}
