// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComponentsAIClaimApp.Data;

internal static class ClaimAgentAddress
{
    internal static Uri Resolve(
        string? configuredBaseAddress,
        string navigationBaseUri)
    {
        var value = string.IsNullOrWhiteSpace(configuredBaseAddress)
            ? navigationBaseUri
            : configuredBaseAddress;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            string.IsNullOrEmpty(uri.Host) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                "ClaimAgent:BaseAddress must be an absolute HTTP or HTTPS URI without a query or fragment.");
        }

        return uri.AbsolutePath.EndsWith('/', StringComparison.Ordinal)
            ? uri
            : new UriBuilder(uri) { Path = $"{uri.AbsolutePath}/" }.Uri;
    }
}
