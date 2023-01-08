// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.SourceGeneration.StaticRouteHandlerModel;

internal static class StaticRouteHandlerModelEmitter
{
    /*
     * TODO: Emit code that represents the signature of the delegate
     * represented by the handler. When the handler does not return a value
     * but consumes parameters the following will be emitted:
     *
     * ```
     * System.Action<string, int>
     * ```
     *
     * Where `string` and `int` represent parameter types. For handlers
     * that do return a value, `System.Func<string, int, string>` will
     * be emitted to indicate a `string`return type.
     */
    public static string EmitHandlerDelegateType(Endpoint endpoint)
    {
        return $"System.Func<{endpoint.Response.ResponseType}>";
    }

    public static string EmitSourceKey(Endpoint endpoint)
    {
        return $@"(@""{endpoint.Location.Item1}"", {endpoint.Location.Item2})";
    }

    /*
     * TODO: Emit invocation to the request handler. The structure
     * involved here consists of a call to bind parameters, check
     * their validity (optionality), invoke the underlying handler with
     * the arguments bound from HTTP context, and write out the response.
     */
    public static string EmitRequestHandler()
    {
        return """
System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                        var result = handler();
                        return httpContext.Response.WriteAsync(result);
                }
""";
    }

    /*
     * TODO: Emit invocation to the `filteredInvocation` pipeline by constructing
     * the `EndpointFilterInvocationContext` using the bound arguments for the handler.
     * In the source generator context, the generic overloads for `EndpointFilterInvocationContext`
     * can be used to reduce the boxing that happens at runtime when constructing
     * the context object.
     */
    public static string EmitFilteredRequestHandler()
    {
        return """
async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext));
                    await ExecuteObjectResult(result, httpContext);
                }
""";
    }

    /*
     * TODO: Emit code that will call the `handler` with
     * the appropriate arguments processed via the parameter binding.
     *
     * ```
     * return System.Threading.Tasks.ValueTask.FromResult<object?>(handler(name, age));
     * ```
     *
     * If the handler returns void, it will be invoked and an `EmptyHttpResult`
     * will be returned to the user.
     *
     * ```
     * handler(name, age);
     * return System.Threading.Tasks.ValueTask.FromResult<object?>(Results.Empty);
     * ```
     */
    public static string EmitFilteredInvocation()
    {
        return "return System.Threading.Tasks.ValueTask.FromResult<object?>(handler());";
    }
}
