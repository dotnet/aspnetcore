// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// This API supports framework infrastructure and is not intended to be used
/// directly from application code.
/// </summary>
public enum HttpMethod : byte
{
    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Get,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Put,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Delete,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Post,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Head,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Trace,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Patch,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Connect,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Options,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Custom,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    None = byte.MaxValue,
}
