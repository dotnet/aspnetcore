// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Interface marking attributes that specify the maximum allowed size of the request body.
/// </summary>
public interface IRequestSizeLimitMetadata
{
    /// <summary>
    /// The maximum allowed size of the current request body in bytes.
    /// </summary>
    long? MaxRequestBodySize { get; }
}
