// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public class EndpointParameterMetadataContext
{
    /// <summary>
    /// 
    /// </summary>
    public ParameterInfo Parameter { get; internal set; } // internal set to allow re-use

    /// <summary>
    /// 
    /// </summary>
    public IServiceProvider? Services { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public IList<object> EndpointMetadata { get; init; }
}
