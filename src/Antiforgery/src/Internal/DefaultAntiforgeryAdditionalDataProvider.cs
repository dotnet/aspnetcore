// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// A default <see cref="IAntiforgeryAdditionalDataProvider"/> implementation.
/// </summary>
internal sealed class DefaultAntiforgeryAdditionalDataProvider : IAntiforgeryAdditionalDataProvider
{
    /// <inheritdoc />
    public string GetAdditionalData(HttpContext context)
    {
        return string.Empty;
    }

    /// <inheritdoc />
    public bool ValidateAdditionalData(HttpContext context, string additionalData)
    {
        // Default implementation does not understand anything but empty data.
        return string.IsNullOrEmpty(additionalData);
    }
}
