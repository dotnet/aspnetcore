// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal static class StaticRouteHandlerModelEmitter
{
    public static string EmitHandlerDelegateType(this Endpoint endpoint)
    {
        if (endpoint.Parameters.Length == 0)
        {
            return endpoint.Response.IsVoid ? "System.Action" : $"System.Func<{endpoint.Response.WrappedResponseType}>";
        }
        else
        {
            var parameterTypeList = string.Join(", ", endpoint.Parameters.Select(p => p.Type.ToDisplayString(EmitterConstants.DisplayFormat)));

            if (endpoint.Response.IsVoid)
            {
                return $"System.Action<{parameterTypeList}>";
            }
            else
            {
                return $"System.Func<{parameterTypeList}, {endpoint.Response.WrappedResponseType}>";
            }
        }
    }

    public static string EmitHandlerDelegateCast(this Endpoint endpoint)
    {
        if (endpoint.Parameters.Length == 0)
        {
            return endpoint.Response.IsVoid ? "Action" : $"Func<{endpoint.Response.WrappedResponseType}>";
        }
        else
        {
            var parameterTypeList = string.Join(", ", endpoint.Parameters.Select(
                p => p.Type.ToDisplayString(p.IsOptional ? NullableFlowState.MaybeNull : NullableFlowState.NotNull, EmitterConstants.DisplayFormat)));

            if (endpoint.Response.IsVoid)
            {
                return $"Action<{parameterTypeList}>";
            }
            else
            {
                return $"Func<{parameterTypeList}, {endpoint.Response.WrappedResponseType}>";
            }
        }
    }

    public static string EmitSourceKey(this Endpoint endpoint)
    {
        return $@"(@""{endpoint.Location.File}"", {endpoint.Location.LineNumber})";
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
        var handlerSignature = endpoint.IsAwaitable ? "async Task RequestHandler(HttpContext httpContext)" : "Task RequestHandler(HttpContext httpContext)";
        var resultAssignment = endpoint.Response.IsVoid ? string.Empty : "var result = ";
        var awaitHandler = endpoint.Response.IsAwaitable ? "await " : string.Empty;
        var setContentType = endpoint.Response.IsVoid ? string.Empty : $@"httpContext.Response.ContentType ??= ""{endpoint.Response.ContentType}"";";

        var requestHandlerSource = $$"""
                    {{handlerSignature}}
                    {
                        var wasParamCheckFailure = false;
{{endpoint.EmitParameterPreparation()}}
                        if (wasParamCheckFailure)
                        {
                            httpContext.Response.StatusCode = 400;
                            {{(endpoint.IsAwaitable ? "return;" : "return Task.CompletedTask;")}}
                        }
                        {{setContentType}}
                        {{resultAssignment}}{{awaitHandler}}handler({{endpoint.EmitArgumentList()}});
                        {{(endpoint.Response.IsVoid ? "return Task.CompletedTask;" : endpoint.EmitResponseWritingCall())}}
                    }
""";

        return requestHandlerSource;
    }

    private static string EmitResponseWritingCall(this Endpoint endpoint)
    {
        var returnOrAwait = endpoint.IsAwaitable ? "await" : "return";

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
    public static string EmitFilteredRequestHandler(this Endpoint endpoint)
    {
        var argumentList = endpoint.Parameters.Length == 0 ? string.Empty : $", {endpoint.EmitArgumentList()}";
        var invocationConstructor = endpoint.Parameters.Length == 0 ? "DefaultEndpointFilterInvocationContext" : "EndpointFilterInvocationContext";
        var invocationGenericArgs = endpoint.Parameters.Length == 0 ? string.Empty : $"<{endpoint.EmitFilterInvocationContextTypeArgs()}>";

        return $$"""
                    async Task RequestHandlerFiltered(HttpContext httpContext)
                    {
                        var wasParamCheckFailure = false;
{{endpoint.EmitParameterPreparation()}}
                        if (wasParamCheckFailure)
                        {
                            httpContext.Response.StatusCode = 400;
                        }
                        var result = await filteredInvocation(new {{invocationConstructor}}{{invocationGenericArgs}}(httpContext{{argumentList}}));
                        await GeneratedRouteBuilderExtensionsCore.ExecuteObjectResult(result, httpContext);
                    }
""";
    }

    public static string EmitFilteredInvocation(this Endpoint endpoint)
    {
        // Note: This string does not need indentation since it is
        // handled when we generate the output string in the `thunks` pipeline.
        return endpoint.Response.IsVoid ? $"""
handler({endpoint.EmitFilteredArgumentList()});
return ValueTask.FromResult<object?>(Results.Empty);
""" : $"""
return ValueTask.FromResult<object?>(handler({endpoint.EmitFilteredArgumentList()}));
""";
    }

    public static string EmitFilteredArgumentList(this Endpoint endpoint)
    {
        if (endpoint.Parameters.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < endpoint.Parameters.Length; i++)
        {
            sb.Append($"ic.GetArgument<{endpoint.Parameters[i].Type}>({i})");

            if (i < endpoint.Parameters.Length - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    public static string EmitFilterInvocationContextTypeArgs(this Endpoint endpoint)
    {
        if (endpoint.Parameters.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < endpoint.Parameters.Length; i++)
        {
            sb.Append(endpoint.Parameters[i].Type.ToDisplayString(endpoint.Parameters[i].IsOptional ? NullableFlowState.MaybeNull : NullableFlowState.NotNull));

            if (i < endpoint.Parameters.Length - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }
}
