// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

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
    public static string EmitHandlerDelegateType(this Endpoint endpoint)
    {
        if (endpoint.Response.IsVoid)
        {
            return $"System.Action";
        }
        if (endpoint.Response.IsAwaitable)
        {
            return $"System.Func<{endpoint.Response.WrappedResponseType}>";
        }
        return $"System.Func<{endpoint.Response.ResponseType}>";
    }

    public static string EmitSourceKey(this Endpoint endpoint)
    {
        return $@"(@""{endpoint.Location.Item1}"", {endpoint.Location.Item2})";
    }

    public static string EmitVerb(this Endpoint endpoint)
    {
        return endpoint.HttpMethod switch
        {
            "MapGet" => "GetVerb",
            "MapPut" => "PutVerb",
            "MapPost" => "PostVerb",
            "MapDelete" => "DeleteVerb",
            "MapPatch" => "PatchVerb",
            _ => throw new ArgumentException($"Received unexpected HTTP method: {endpoint.HttpMethod}")
        };
    }

    /*
     * TODO: Emit invocation to the request handler. The structure
     * involved here consists of a call to bind parameters, check
     * their validity (optionality), invoke the underlying handler with
     * the arguments bound from HTTP context, and write out the response.
     */
    public static string EmitRequestHandler(this Endpoint endpoint)
    {
        var handlerSignature = endpoint.Response.IsAwaitable ? "async Task RequestHandler(HttpContext httpContext)" : "Task RequestHandler(HttpContext httpContext)";
        var resultAssignment = endpoint.Response.IsVoid ? string.Empty : "var result = ";
        var awaitHandler = endpoint.Response.IsAwaitable ? "await " : string.Empty;
        var setContentType = endpoint.Response.IsVoid ? string.Empty : $@"httpContext.Response.ContentType ??= ""{endpoint.Response.ContentType}"";";
        return $$"""
                    {{handlerSignature}}
                    {
                        {{setContentType}}
                        {{resultAssignment}}{{awaitHandler}}handler();
                        {{(endpoint.Response.IsVoid ? "return Task.CompletedTask;" : endpoint.EmitResponseWritingCall())}}
                    }
""";
    }

    private static string EmitResponseWritingCall(this Endpoint endpoint)
    {
        var returnOrAwait = endpoint.Response.IsAwaitable ? "await" : "return";

        if (endpoint.Response.IsIResult)
        {
            return $"{returnOrAwait} result.ExecuteAsync(httpContext);";
        }
        else if (endpoint.Response.ResponseType.SpecialType == SpecialType.System_String)
        {
            return $"{returnOrAwait} httpContext.Response.WriteAsync(result);";
        }
        else if (endpoint.Response.ResponseType.SpecialType == SpecialType.System_Object)
        {
            return $"{returnOrAwait} GeneratedRouteBuilderExtensionsCore.ExecuteObjectResult(result, httpContext);";
        }
        else if (!endpoint.Response.IsVoid)
        {
            return $"{returnOrAwait} httpContext.Response.WriteAsJsonAsync(result);";
        }
        else if (!endpoint.Response.IsAwaitable && endpoint.Response.IsVoid)
        {
            return $"{returnOrAwait} Task.CompletedTask;";
        }
        else
        {
            return $"{returnOrAwait} httpContext.Response.WriteAsync(result);";
        }
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
                    async Task RequestHandlerFiltered(HttpContext httpContext)
                    {
                        var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext));
                        await GeneratedRouteBuilderExtensionsCore.ExecuteObjectResult(result, httpContext);
                    }
""";
    }

    /*
     * TODO: Emit code that will call the `handler` with
     * the appropriate arguments processed via the parameter binding.
     *
     * ```
     * return ValueTask.FromResult<object?>(handler(name, age));
     * ```
     *
     * If the handler returns void, it will be invoked and an `EmptyHttpResult`
     * will be returned to the user.
     *
     * ```
     * handler(name, age);
     * return ValueTask.FromResult<object?>(Results.Empty);
     * ```
     */
    public static string EmitFilteredInvocation(this Endpoint endpoint)
    {
        // Note: This string does not need indentation since it is
        // handled when we generate the output string in the `thunks` pipeline.
        return endpoint.Response.IsVoid ? """
handler();
return ValueTask.FromResult<object?>(Results.Empty);
""" : """
return ValueTask.FromResult<object?>(handler());
""";
    }
}
