// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Used to hold per-request state.
/// </summary>
internal sealed class AntiforgeryFeature : IAntiforgeryFeature
{
    public bool HaveDeserializedCookieToken { get; set; }

    public AntiforgeryToken? CookieToken { get; set; }

    public bool HaveDeserializedRequestToken { get; set; }

    public AntiforgeryToken? RequestToken { get; set; }

    public bool HaveGeneratedNewCookieToken { get; set; }

    // After HaveGeneratedNewCookieToken is true, remains null if CookieToken is valid.
    public AntiforgeryToken? NewCookieToken { get; set; }

    // After HaveGeneratedNewCookieToken is true, remains null if CookieToken is valid.
    public string? NewCookieTokenString { get; set; }

    public AntiforgeryToken? NewRequestToken { get; set; }

    public string? NewRequestTokenString { get; set; }

    // Always false if NewCookieToken is null. Never store null cookie token or re-store cookie token from request.
    public bool HaveStoredNewCookieToken { get; set; }
}
