// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Represents the feature that provides the current request's culture information.
/// </summary>
public interface IRequestCultureFeature
{
    /// <summary>
    /// The <see cref="Localization.RequestCulture"/> of the request.
    /// </summary>
    RequestCulture RequestCulture { get; }

    /// <summary>
    /// The <see cref="IRequestCultureProvider"/> that determined the request's culture information.
    /// If the value is <c>null</c> then no provider was used and the request's culture was set to the value of
    /// <see cref="Builder.RequestLocalizationOptions.DefaultRequestCulture"/>.
    /// </summary>
    IRequestCultureProvider? Provider { get; }
}
