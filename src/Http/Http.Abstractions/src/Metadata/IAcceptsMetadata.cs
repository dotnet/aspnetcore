// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Interface for accepting request media types.
/// </summary>
public interface IAcceptsMetadata
{
    /// <summary>
    /// Gets a list of the allowed request content types.
    /// If the incoming request contains a <c>Content-Type</c> and the content type is not
    /// one of these values, the request will be rejected with a 415 response. If the
    /// incoming request does not contain a <c>Content-Type</c> header, the content type
    /// check will be bypassed.
    /// </summary>
    IReadOnlyList<string> ContentTypes { get; }

    /// <summary>
    /// Gets the type being read from the request. 
    /// </summary>
    Type? RequestType { get; }

    /// <summary>
    /// Gets a value that determines if the request body is optional.
    /// </summary>
    bool IsOptional { get; }
}
