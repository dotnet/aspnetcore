// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// Internal interfaces that allows us to optimize the request execution path based on ActionDescriptor
/// </summary>
internal interface IRequestDelegateFactory
{
    RequestDelegate? CreateRequestDelegate(ActionDescriptor actionDescriptor, RouteValueDictionary? dataTokens);
}
