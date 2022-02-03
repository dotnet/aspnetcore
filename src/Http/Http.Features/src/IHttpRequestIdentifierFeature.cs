// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Feature to uniquely identify a request.
/// </summary>
public interface IHttpRequestIdentifierFeature
{
    /// <summary>
    /// Gets or sets a value to uniquely identify a request.
    /// This can be used for logging and diagnostics.
    /// </summary>
    string TraceIdentifier { get; set; }
}
