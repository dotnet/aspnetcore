// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandler.Model;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandler.Emitters;

internal static class EndpointEmitter
{
    public static string EmitHandlerDelegateType(this Endpoint endpoint, bool considerOptionality = false)
    {
        // Emits a delegate type to use when casting the input that captures
        // default parameter values.
        //
        // void (int arg0, Todo arg1) => throw null!
        // IResult (int arg0, Todo arg1) => throw null!
        if (endpoint.Parameters.Length == 0)
        {
            return endpoint.Response == null || (endpoint.Response.HasNoResponse && !endpoint.Response.IsAwaitable) ? "void ()" : $"{endpoint.Response.WrappedResponseType} ()";
        }
        var parameterTypeList = string.Join(", ", endpoint.Parameters.Select((p, i) => $"{getType(p, considerOptionality)} arg{i}{(p.HasDefaultValue ? $"= {p.DefaultValue}" : string.Empty)}"));

        if (endpoint.Response == null || (endpoint.Response.HasNoResponse && !endpoint.Response.IsAwaitable))
        {
            return $"void ({parameterTypeList})";
        }
        return $"{endpoint.Response.WrappedResponseType} ({parameterTypeList})";

        static string getType(EndpointParameter p, bool considerOptionality)
        {
            return considerOptionality
                ? p.Type.ToDisplayString(p.IsOptional ? NullableFlowState.MaybeNull : NullableFlowState.NotNull, EmitterConstants.DisplayFormat)
                : p.Type.ToDisplayString(EmitterConstants.DisplayFormat);
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
            "MapMethods" => "httpMethods",
            "Map" => "null",
            "MapFallback" => "null",
            _ => throw new ArgumentException($"Received unexpected HTTP method: {endpoint.HttpMethod}")
        };
    }

    /*
     * Emit invocation to the request handler. The structure
     * involved here consists of a call to bind parameters, check
     * their validity (optionality), invoke the underlying handler with
     * the arguments bound from HTTP context, and write out the response.
     */
    public static void EmitRequestHandler(this Endpoint endpoint, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpoint.IsAwaitable ? "async Task RequestHandler(HttpContext httpContext)" : "Task RequestHandler(HttpContext httpContext)");
        codeWriter.StartBlock(); // Start handler method block
        codeWriter.WriteLine("var wasParamCheckFailure = false;");

        if (endpoint.Parameters.Length > 0)
        {
            codeWriter.WriteLine(endpoint.Parameters.EmitParameterPreparation(endpoint.EmitterContext, codeWriter.Indent));
        }

        codeWriter.WriteLine("if (wasParamCheckFailure)");
        codeWriter.StartBlock(); // Start if-statement block
        codeWriter.WriteLine("httpContext.Response.StatusCode = 400;");
        codeWriter.WriteLine(endpoint.IsAwaitable ? "return;" : "return Task.CompletedTask;");
        codeWriter.EndBlock(); // End if-statement block
        if (endpoint.Response == null)
        {
            return;
        }
        if (!endpoint.Response.HasNoResponse && endpoint.Response is { ContentType: { } contentType })
        {
            codeWriter.WriteLine($@"httpContext.Response.ContentType ??= ""{contentType}"";");
        }
        if (!endpoint.Response.HasNoResponse)
        {
            codeWriter.Write("var result = ");
        }
        if (endpoint.Response.IsAwaitable)
        {
            codeWriter.Write("await ");
        }
        codeWriter.WriteLine($"handler({endpoint.EmitArgumentList()});");
        if (!endpoint.Response.HasNoResponse)
        {
            codeWriter.WriteLine(endpoint.Response.EmitResponseWritingCall(endpoint.IsAwaitable));
        }
        else if (!endpoint.IsAwaitable)
        {
            codeWriter.WriteLine("return Task.CompletedTask;");
        }
        codeWriter.EndBlock(); // End handler method block
    }

    public static void EmitFilteredRequestHandler(this Endpoint endpoint, CodeWriter codeWriter)
    {
        var argumentList = endpoint.Parameters.Length == 0 ? string.Empty : $", {endpoint.EmitArgumentList()}";
        var invocationCreator = endpoint.Parameters.Length > 8
            ? "new DefaultEndpointFilterInvocationContext"
            : "EndpointFilterInvocationContext.Create";
        var invocationGenericArgs = endpoint.Parameters.Length is > 0 and < 8
            ? $"<{endpoint.EmitFilterInvocationContextTypeArgs()}>"
            : string.Empty;

        codeWriter.WriteLine("async Task RequestHandlerFiltered(HttpContext httpContext)");
        codeWriter.StartBlock(); // Start handler method block
        codeWriter.WriteLine("var wasParamCheckFailure = false;");

        if (endpoint.Parameters.Length > 0)
        {
            codeWriter.WriteLine(endpoint.Parameters.EmitParameterPreparation(endpoint.EmitterContext, codeWriter.Indent));
        }

        codeWriter.WriteLine("if (wasParamCheckFailure)");
        codeWriter.StartBlock(); // Start if-statement block
        codeWriter.WriteLine("httpContext.Response.StatusCode = 400;");
        codeWriter.EndBlock(); // End if-statement block
        codeWriter.WriteLine($"var result = await filteredInvocation({invocationCreator}{invocationGenericArgs}(httpContext{argumentList}));");
        codeWriter.WriteLine("await GeneratedRouteBuilderExtensionsCore.ExecuteObjectResult(result, httpContext);");
        codeWriter.EndBlock(); // End handler method block
    }

    public static void EmitEndpointMetadataPopulation(this Endpoint endpoint, CodeWriter codeWriter)
    {
        endpoint.EmitAcceptsMetadata(codeWriter);
        endpoint.EmitBuiltinResponseTypeMetadata(codeWriter);
        endpoint.EmitCallsToMetadataProvidersForParameters(codeWriter);
        endpoint.EmitCallToMetadataProviderForResponse(codeWriter);
    }

    public static void EmitFilteredInvocation(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.Response?.HasNoResponse == true)
        {
            codeWriter.WriteLine(endpoint.Response?.IsAwaitable == true
                ? $"await handler({endpoint.EmitFilteredArgumentList()});"
                : $"handler({endpoint.EmitFilteredArgumentList()});");
            codeWriter.WriteLine(endpoint.Response?.IsAwaitable == true
                ? "return (object?)Results.Empty;"
                : "return ValueTask.FromResult<object?>(Results.Empty);");
        }
        else if (endpoint.Response?.IsAwaitable == true)
        {
            codeWriter.WriteLine($"var result = await handler({endpoint.EmitFilteredArgumentList()});");
            codeWriter.WriteLine("return (object?)result;");
        }
        else
        {
            codeWriter.WriteLine($"return ValueTask.FromResult<object?>(handler({endpoint.EmitFilteredArgumentList()}));");
        }
    }

    private static string EmitFilteredArgumentList(this Endpoint endpoint)
    {
        if (endpoint.Parameters.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < endpoint.Parameters.Length; i++)
        {
            // The null suppression operator on the GetArgument(...) call here is required because we'll occassionally be
            // dealing with nullable types here. We could try to do fancy things to branch the logic here depending on
            // the nullability, but at the end of the day we are going to call GetArguments(...) - at runtime the nullability
            // suppression operator doesn't come into play - so its not worth worrying about.
            sb.Append($"ic.GetArgument<{endpoint.Parameters[i].Type.ToDisplayString(EmitterConstants.DisplayFormat)}>({i})!");

            if (i < endpoint.Parameters.Length - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    private static string EmitFilterInvocationContextTypeArgs(this Endpoint endpoint)
    {
        if (endpoint.Parameters.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < endpoint.Parameters.Length; i++)
        {
            sb.Append(endpoint.Parameters[i].Type.ToDisplayString(endpoint.Parameters[i].IsOptional ? NullableFlowState.MaybeNull : NullableFlowState.NotNull, EmitterConstants.DisplayFormat));

            if (i < endpoint.Parameters.Length - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    private static string EmitArgumentList(this Endpoint endpoint) => string.Join(", ", endpoint.Parameters.Select(p => p.EmitArgument()));
}
