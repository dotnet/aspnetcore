// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Result;

public class EmptyResultTest
{
    public async Task EmptyResult_DoesNothing()
    {
        var emptyResult = new EmptyResult();

        // Assert- does not throw.
        await emptyResult.ExecuteAsync(GetHttpContext());
    }

    private static HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices();
        return httpContext;
    }

    private static IServiceProvider CreateServices()
    {
        return new ServiceCollection().BuildServiceProvider();
    }
}
