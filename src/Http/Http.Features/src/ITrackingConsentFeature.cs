// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Used to query, grant, and withdraw user consent regarding the storage of user
/// information related to site activity and functionality.
/// </summary>
public interface ITrackingConsentFeature
{
    /// <summary>
    /// Indicates if consent is required for the given request.
    /// </summary>
    bool IsConsentNeeded { get; }

    /// <summary>
    /// Indicates if consent was given.
    /// </summary>
    bool HasConsent { get; }

    /// <summary>
    /// Indicates either if consent has been given or if consent is not required.
    /// </summary>
    bool CanTrack { get; }

    /// <summary>
    /// Grants consent for this request. If the response has not yet started then
    /// this will also grant consent for future requests.
    /// </summary>
    void GrantConsent();

    /// <summary>
    /// Withdraws consent for this request. If the response has not yet started then
    /// this will also withdraw consent for future requests.
    /// </summary>
    void WithdrawConsent();

    /// <summary>
    /// Creates a consent cookie for use when granting consent from a javascript client.
    /// </summary>
    string CreateConsentCookie();
}
