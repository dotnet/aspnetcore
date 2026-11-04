// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// Interface that exposes a list of http methods that are supported by an provider.
/// </summary>
public interface IActionHttpMethodProvider
{
    /// <summary>
    /// The list of http methods this action provider supports.
    /// </summary>
    IEnumerable<string> HttpMethods { get; }
}
