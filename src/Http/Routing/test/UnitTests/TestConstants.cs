// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

public static class TestConstants
{
    internal static readonly RequestDelegate EmptyRequestDelegate = (context) => Task.CompletedTask;
    internal static readonly RequestDelegate ShortCircuitRequestDelegate = (context) =>
    {
        context.Items["ShortCircuit"] = true;
        return Task.CompletedTask;
    };
}
