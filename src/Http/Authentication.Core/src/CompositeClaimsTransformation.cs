// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Composes multiple <see cref="IClaimsTransformation"/> instances, executing them sequentially
/// in the order they were registered.
/// </summary>
internal sealed class CompositeClaimsTransformation : IClaimsTransformation
{
    private readonly IEnumerable<IClaimsTransformation> _transformations;

    public CompositeClaimsTransformation(IEnumerable<IClaimsTransformation> transformations)
    {
        _transformations = transformations;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        foreach (var transformation in _transformations)
        {
            principal = await transformation.TransformAsync(principal);
        }

        return principal;
    }
}
