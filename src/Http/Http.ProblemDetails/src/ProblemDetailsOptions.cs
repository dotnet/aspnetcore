// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public class ProblemDetailsOptions
{
    /// <summary>
    /// 
    /// </summary>
    public bool SuppressMapRoutingErrors { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    public bool SuppressMapClientErrors { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    public bool SuppressMapExceptions { get; set; } = true;

    internal Dictionary<int, ProblemDetailsErrorData> ProblemDetailsErrorMapping { get; } = new Dictionary<int, ProblemDetailsErrorData>();
}
