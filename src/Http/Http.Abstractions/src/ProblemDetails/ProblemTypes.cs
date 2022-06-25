// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
[Flags]
public enum ProblemTypes : uint
{
    /// <summary>
    /// 
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 
    /// </summary>
    Server = 1,

    /// <summary>
    /// 404 / 405 / 415
    /// </summary>
    Routing = 2,

    /// <summary>
    /// 
    /// </summary>
    Client = 4,

    /// <summary>
    /// 
    /// </summary>
    All = Routing | Server | Client,
}
