// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides information about rejected HTTP requests.
/// </summary>
public interface IBadRequestExceptionFeature
{
    /// <summary>
    /// Synchronously retrieves the exception associated with the rejected HTTP request.
    /// </summary>
    Exception? Error { get; }
}
