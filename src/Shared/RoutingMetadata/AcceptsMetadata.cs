// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Metadata that specifies the supported request content types.
/// </summary>
internal sealed class AcceptsMetadata : IAcceptsMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="AcceptsMetadata"/>.
    /// </summary>
    public AcceptsMetadata(string[] contentTypes)
    {
        ArgumentNullException.ThrowIfNull(contentTypes);

        ContentTypes = contentTypes;
    }

    /// <summary>
    /// Creates a new instance of <see cref="AcceptsMetadata"/> with a type.
    /// </summary>
    public AcceptsMetadata(Type? type, bool isOptional, string[] contentTypes)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(contentTypes);

        RequestType = type;
        ContentTypes = contentTypes;
        IsOptional = isOptional;
    }

    /// <summary>
    /// Gets the supported request content types.
    /// </summary>
    public IReadOnlyList<string> ContentTypes { get; }

    /// <summary>
    /// Gets the type being read from the request.
    /// </summary>
    public Type? RequestType { get; }

    /// <summary>
    /// Gets a value that determines if the request body is optional.
    /// </summary>
    public bool IsOptional { get; }
}
