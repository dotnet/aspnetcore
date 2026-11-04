// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Represents an exception handler with the original path of the request.
/// </summary>
public interface IExceptionHandlerPathFeature : IExceptionHandlerFeature
{
    /// <summary>
    /// The portion of the request path that identifies the requested resource. The value
    /// is un-escaped.
    /// </summary>
    new string Path => ((IExceptionHandlerFeature)this).Path;
}
