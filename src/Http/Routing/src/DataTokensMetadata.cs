// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Metadata that defines data tokens for an <see cref="Endpoint"/>. This metadata
/// type provides data tokens value for <see cref="RouteData.DataTokens"/> associated
/// with an endpoint.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DataTokensMetadata : IDataTokensMetadata
{
    /// <summary>
    /// Constructor for a new <see cref="DataTokensMetadata"/> given <paramref name="dataTokens"/>.
    /// </summary>
    /// <param name="dataTokens">The data tokens.</param>
    public DataTokensMetadata(IReadOnlyDictionary<string, object?> dataTokens)
    {
        DataTokens = dataTokens ?? throw new ArgumentNullException(nameof(dataTokens));
    }

    /// <summary>
    /// Get the data tokens.
    /// </summary>
    public IReadOnlyDictionary<string, object?> DataTokens { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(DataTokens), DataTokens.Select(t => $"{t.Key}={t.Value ?? "(null)"}"));
    }
}
