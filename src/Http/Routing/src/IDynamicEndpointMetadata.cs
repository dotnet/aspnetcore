// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// A metadata interface that can be used to specify that the associated <see cref="Endpoint" />
/// will be dynamically replaced during matching.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IDynamicEndpointMetadata"/> and related derived interfaces signal to
/// <see cref="MatcherPolicy"/> implementations that an <see cref="Endpoint"/> has dynamic behavior
/// and thus cannot have its characteristics cached.
/// </para>
/// <para>
/// Using dynamic endpoints can be useful because the default matcher implementation does not
/// supply extensibility for how URLs are processed. Routing implementations that have dynamic
/// behavior can apply their dynamic logic after URL processing, by replacing a endpoints as
/// part of a <see cref="CandidateSet"/>.
/// </para>
/// </remarks>
public interface IDynamicEndpointMetadata
{
    /// <summary>
    /// Returns a value that indicates whether the associated endpoint has dynamic matching
    /// behavior.
    /// </summary>
    bool IsDynamic { get; }
}
