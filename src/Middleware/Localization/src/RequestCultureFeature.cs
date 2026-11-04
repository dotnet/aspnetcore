// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Provides the current request's culture information.
/// </summary>
public class RequestCultureFeature : IRequestCultureFeature
{
    /// <summary>
    /// Creates a new <see cref="RequestCultureFeature"/> with the specified <see cref="Localization.RequestCulture"/>.
    /// </summary>
    /// <param name="requestCulture">The <see cref="Localization.RequestCulture"/>.</param>
    /// <param name="provider">The <see cref="IRequestCultureProvider"/>.</param>
    public RequestCultureFeature(RequestCulture requestCulture, IRequestCultureProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(requestCulture);

        RequestCulture = requestCulture;
        Provider = provider;
    }

    /// <inheritdoc />
    public RequestCulture RequestCulture { get; }

    /// <inheritdoc />
    public IRequestCultureProvider? Provider { get; }
}
