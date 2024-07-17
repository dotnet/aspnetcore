// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal static class StaticRouteHandlerModelEmitter
{
    public static string EmitHandlerDelegateType(this Endpoint endpoint)
    {
        // Emits a delegate type to use when casting the input that captures
        // default parameter values.
        //
        // void (int arg0, Todo arg1) => throw null!
        // IResult (int arg0, Todo arg1) => throw null!
        if (endpoint.Parameters.Length == 0)
        {
            return endpoint.Response == null || (endpoint.Response.HasNoResponse && !endpoint.Response.IsAwaitable) ? "void ()" : $"{endpoint.Response.WrappedResponseTypeDisplayName} ()";
        }
        var parameterTypeList = string.Join(", ", endpoint.Parameters.Select((p, i) => $"{EmitUnwrappedParameterType(p)} arg{i}{(p.HasDefaultValue ? $"= {p.DefaultValue}" : string.Empty)}"));

        if (endpoint.Response == null || (endpoint.Response.HasNoResponse && !endpoint.Response.IsAwaitable))
        {
            return $"void ({parameterTypeList})";
        }
        return $"{endpoint.Response.WrappedResponseTypeDisplayName} ({parameterTypeList})";
    }

    private static string EmitUnwrappedParameterType(EndpointParameter p)
    {
        var type = p.UnwrapParameterType();
        var isOptional = p.IsOptional || type.NullableAnnotation == NullableAnnotation.Annotated;
        return type.ToDisplayString(isOptional ? NullableFlowState.MaybeNull : NullableFlowState.NotNull, EmitterConstants.DisplayFormat);
    }

    public static string EmitVerb(this Endpoint endpoint)
    {
        (var verbSymbol, endpoint.EmitterContext.HttpMethod) = endpoint.HttpMethod switch
        {
            "MapGet" => ("GetVerb", "Get"),
            "MapPut" => ("PutVerb", "Put"),
            "MapPost" => ("PostVerb", "Post"),
            "MapDelete" => ("DeleteVerb", "Delete"),
            "MapPatch" => ("PatchVerb", "Patch"),
            "MapMethods" => ("httpMethods", null),
            "Map" => ("null", null),
            "MapFallback" => ("null", null),
            _ => throw new ArgumentException($"Received unexpected HTTP method: {endpoint.HttpMethod}")
        };

        return verbSymbol;
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
            codeWriter.WriteLineNoTabs(endpoint.Parameters.EmitParameterPreparation(endpoint.EmitterContext, codeWriter.Indent));
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
        if (endpoint.Response.IsAwaitable)
        {
            codeWriter.WriteLine($"var task = handler({endpoint.EmitArgumentList()});");
        }
        if (endpoint.Response.IsAwaitable && endpoint.Response.WrappedResponseType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            codeWriter.WriteLine("if (task == null)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("""throw new InvalidOperationException("The Task returned by the Delegate must not be null.");""");
            codeWriter.EndBlock();
        }
        if (!endpoint.Response.HasNoResponse)
        {
            codeWriter.Write("var result = ");
        }
        codeWriter.WriteLine(endpoint.Response.IsAwaitable ? "await task;" : $"handler({endpoint.EmitArgumentList()});");

        endpoint.Response.EmitHttpResponseContentType(codeWriter);

        if (!endpoint.Response.HasNoResponse)
        {
            endpoint.Response.EmitResponseWritingCall(codeWriter, endpoint.IsAwaitable);
        }
        else if (!endpoint.IsAwaitable)
        {
            codeWriter.WriteLine("return Task.CompletedTask;");
        }

        codeWriter.EndBlock(); // End handler method block
    }

    private static void EmitHttpResponseContentType(this EndpointResponse endpointResponse, CodeWriter codeWriter)
    {
        if (!endpointResponse.HasNoResponse
            && endpointResponse.ResponseType is { } responseType
            && (responseType.SpecialType == SpecialType.System_Object || responseType.SpecialType == SpecialType.System_String))
        {
            codeWriter.WriteLine("if (result is string)");
            codeWriter.StartBlock();
            codeWriter.WriteLine($@"httpContext.Response.ContentType ??= ""text/plain; charset=utf-8"";");
            codeWriter.EndBlock();
            codeWriter.WriteLine("else");
            codeWriter.StartBlock();
            codeWriter.WriteLine($@"httpContext.Response.ContentType ??= ""application/json; charset=utf-8"";");
            codeWriter.EndBlock();
        }
    }

    private static void EmitResponseWritingCall(this EndpointResponse endpointResponse, CodeWriter codeWriter, bool isAwaitable)
    {
        var returnOrAwait = isAwaitable ? "await" : "return";

        if (endpointResponse.IsIResult)
        {
            codeWriter.WriteLine("if (result == null)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("""throw new InvalidOperationException("The IResult returned by the Delegate must not be null.");""");
            codeWriter.EndBlock();
            codeWriter.WriteLine($"{returnOrAwait} GeneratedRouteBuilderExtensionsCore.ExecuteAsyncExplicit(result, httpContext);");
        }
        else if (endpointResponse.ResponseType?.SpecialType == SpecialType.System_String)
        {
            codeWriter.WriteLine($"{returnOrAwait} httpContext.Response.WriteAsync(result);");
        }
        else if (endpointResponse.ResponseType?.SpecialType == SpecialType.System_Object)
        {
            codeWriter.WriteLine($"{returnOrAwait} GeneratedRouteBuilderExtensionsCore.ExecuteReturnAsync(result, httpContext, objectJsonTypeInfo);");
        }
        else if (!endpointResponse.HasNoResponse)
        {
            codeWriter.WriteLine($"{returnOrAwait} {endpointResponse.EmitJsonResponse()}");
        }
        else if (!endpointResponse.IsAwaitable && endpointResponse.HasNoResponse)
        {
            codeWriter.WriteLine($"{returnOrAwait} Task.CompletedTask;");
        }
        else
        {
            codeWriter.WriteLine($"{returnOrAwait} httpContext.Response.WriteAsync(result);");
        }
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
            codeWriter.WriteLineNoTabs(endpoint.Parameters.EmitParameterPreparation(endpoint.EmitterContext, codeWriter.Indent));
        }

        codeWriter.WriteLine("if (wasParamCheckFailure)");
        codeWriter.StartBlock(); // Start if-statement block
        codeWriter.WriteLine("httpContext.Response.StatusCode = 400;");
        codeWriter.EndBlock(); // End if-statement block
        codeWriter.WriteLine($"var result = await filteredInvocation({invocationCreator}{invocationGenericArgs}(httpContext{argumentList}));");
        codeWriter.WriteLine("if (result is not null)");
        codeWriter.StartBlock();
        codeWriter.WriteLine("await GeneratedRouteBuilderExtensionsCore.ExecuteReturnAsync(result, httpContext, objectJsonTypeInfo);");
        codeWriter.EndBlock();
        codeWriter.EndBlock(); // End handler method block
    }

    private static void EmitBuiltinResponseTypeMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.Response is not { } response)
        {
            return;
        }

        if (!endpoint.Response.IsAwaitable && (response.HasNoResponse || response.IsIResult))
        {
            return;
        }

        endpoint.EmitterContext.HasResponseMetadata = true;
        if (response.ResponseType?.SpecialType == SpecialType.System_String)
        {
            codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(statusCode: StatusCodes.Status200OK, type: typeof(string), contentTypes: GeneratedMetadataConstants.PlaintextContentType));");
        }
        else if (response.IsAwaitable && response.ResponseType == null)
        {
            codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(statusCode: StatusCodes.Status200OK, type: typeof(void), contentTypes: GeneratedMetadataConstants.PlaintextContentType));");
        }
        else if (response.ResponseType is { } responseType)
        {
            codeWriter.WriteLine($$"""options.EndpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(statusCode: StatusCodes.Status200OK, type: typeof({{responseType.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability)}}), contentTypes: GeneratedMetadataConstants.JsonContentType));""");
        }
    }

    private static void EmitCallToMetadataProviderForResponse(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.Response is not { } response || response.ResponseType is not { } responseType)
        {
            return;
        }

        if (response.IsEndpointMetadataProvider)
        {
            codeWriter.WriteLine($"PopulateMetadataForEndpoint<{responseType.ToDisplayString(EmitterConstants.DisplayFormat)}>(methodInfo, options.EndpointBuilder);");
        }
    }
    private static void EmitCallsToMetadataProvidersForParameters(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.EmitterContext.HasEndpointParameterMetadataProvider)
        {
            codeWriter.WriteLine("var parameterInfos = methodInfo.GetParameters();");
        }

        foreach (var parameter in endpoint.Parameters)
        {
            if (parameter is { Source: EndpointParameterSource.AsParameters, EndpointParameters: { } innerParameters })
            {
                foreach (var innerParameter in innerParameters)
                {
                    ProcessParameter(innerParameter, codeWriter);
                }
            }

            // Even if a parameter is decorated with the AsParameters attribute, we still need
            // to fetch metadata on the parameter itself (as well as the properties).
            ProcessParameter(parameter, codeWriter);
        }

        static void ProcessParameter(EndpointParameter parameter, CodeWriter codeWriter)
        {
            if (parameter.Type is not { } parameterType)
            {
                return;
            }

            if (parameter.IsEndpointParameterMetadataProvider)
            {
                var resolveParameterInfo = parameter.IsProperty
                    ? parameter.PropertyAsParameterInfoConstruction
                    : $"parameterInfos[{parameter.Ordinal}]";
                codeWriter.WriteLine($"var {parameter.SymbolName}_ParameterInfo = {resolveParameterInfo};");
                codeWriter.WriteLine($"PopulateMetadataForParameter<{parameterType.ToDisplayString(EmitterConstants.DisplayFormat)}>({parameter.SymbolName}_ParameterInfo, options.EndpointBuilder);");
            }

            if (parameter.IsEndpointMetadataProvider)
            {
                codeWriter.WriteLine($"PopulateMetadataForEndpoint<{parameterType.ToDisplayString(EmitterConstants.DisplayFormat)}>(methodInfo, options.EndpointBuilder);");
            }

        }
    }

    public static void EmitFormAcceptsMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        var hasFormFiles = endpoint.Parameters.Any(p => p.IsFormFile);

        if (hasFormFiles)
        {
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(new AcceptsMetadata(contentTypes: GeneratedMetadataConstants.FormFileContentType));");
        }
        else
        {
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(new AcceptsMetadata(contentTypes: GeneratedMetadataConstants.FormContentType));");
        }
    }

    public static void EmitJsonAcceptsMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        EndpointParameter? explicitBodyParameter = null;
        var potentialImplicitBodyParameters = new List<EndpointParameter>();

        foreach (var parameter in endpoint.Parameters)
        {
            if (explicitBodyParameter == null && parameter.Source == EndpointParameterSource.JsonBody)
            {
                explicitBodyParameter = parameter;
                break;
            }
            else if (parameter.Source == EndpointParameterSource.JsonBodyOrService || parameter.Source == EndpointParameterSource.JsonBodyOrQuery)
            {
                potentialImplicitBodyParameters.Add(parameter);
            }
        }

        if (explicitBodyParameter != null)
        {
            codeWriter.WriteLine($$"""options.EndpointBuilder.Metadata.Add(new AcceptsMetadata(type: typeof({{explicitBodyParameter.Type.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability)}}), isOptional: {{(explicitBodyParameter.IsOptional ? "true" : "false")}}, contentTypes: GeneratedMetadataConstants.JsonContentType));""");
        }
        else if (potentialImplicitBodyParameters.Count > 0)
        {
            codeWriter.WriteLine("var serviceProvider = options.ServiceProvider ?? options.EndpointBuilder.ApplicationServices;");
            codeWriter.WriteLine($"var serviceProviderIsService = serviceProvider.GetRequiredService<IServiceProviderIsService>();");

            codeWriter.WriteLine("var jsonBodyOrServiceTypeTuples = new (bool, Type)[] {");
            codeWriter.Indent++;
            codeWriter.WriteLine("#nullable disable");
            foreach (var parameter in potentialImplicitBodyParameters)
            {
                codeWriter.WriteLine($$"""({{(parameter.IsOptional ? "true" : "false")}}, typeof({{parameter.Type.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability)}})),""");
            }
            codeWriter.WriteLine("#nullable enable");
            codeWriter.Indent--;
            codeWriter.WriteLine("};");
            codeWriter.WriteLine("foreach (var (isOptional, type) in jsonBodyOrServiceTypeTuples)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("if (!serviceProviderIsService.IsService(type))");
            codeWriter.StartBlock();
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(new AcceptsMetadata(type: type, isOptional: isOptional, contentTypes: GeneratedMetadataConstants.JsonContentType));");
            codeWriter.WriteLine("break;");
            codeWriter.EndBlock();
            codeWriter.EndBlock();
        }
        else
        {
            codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new AcceptsMetadata(contentTypes: GeneratedMetadataConstants.JsonContentType));");
        }
    }

    public static void EmitAcceptsMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        var hasJsonBody = endpoint.EmitterContext.HasJsonBody || endpoint.EmitterContext.HasJsonBodyOrService || endpoint.EmitterContext.HasJsonBodyOrQuery;

        if (endpoint.EmitterContext.HasFormBody)
        {
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(AntiforgeryMetadata.ValidationRequired);");
            endpoint.EmitFormAcceptsMetadata(codeWriter);
        }
        else if (hasJsonBody)
        {
            endpoint.EmitJsonAcceptsMetadata(codeWriter);
        }
    }

    public static void EmitParameterBindingMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        foreach (var parameter in endpoint.Parameters)
        {
            endpoint.EmitterContext.RequiresParameterBindingMetadataClass = true;
            if (parameter.EndpointParameters is not null)
            {
                foreach (var propertyAsParameter in parameter.EndpointParameters)
                {
                    EmitParameterBindingMetadataForParameter(propertyAsParameter, codeWriter);
                }
            }
            else
            {
                EmitParameterBindingMetadataForParameter(parameter, codeWriter);
            }
        }

        static void EmitParameterBindingMetadataForParameter(EndpointParameter parameter, CodeWriter codeWriter)
        {
            var parameterName = SymbolDisplay.FormatLiteral(parameter.SymbolName, true);
            var parameterInfo = parameter.IsProperty ? parameter.PropertyAsParameterInfoConstruction : $"methodInfo.GetParameters()[{parameter.Ordinal}]";
            var hasTryParse = parameter.IsParsable ? "true" : "false";
            var hasBindAsync = parameter.Source == EndpointParameterSource.BindAsync ? "true" : "false";
            var isOptional = parameter.IsOptional ? "true" : "false";
            codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new ParameterBindingMetadata({parameterName}, {parameterInfo}, hasTryParse: {hasTryParse}, hasBindAsync: {hasBindAsync}, isOptional: {isOptional}));");
        }
    }

    public static void EmitEndpointMetadataPopulation(this Endpoint endpoint, CodeWriter codeWriter)
    {
        endpoint.EmitAcceptsMetadata(codeWriter);
        endpoint.EmitParameterBindingMetadata(codeWriter);
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
            codeWriter.WriteLine($"var task = handler({endpoint.EmitFilteredArgumentList()});");
            if (endpoint.Response?.WrappedResponseType.NullableAnnotation == NullableAnnotation.Annotated)
            {
                codeWriter.WriteLine("if (task == null)");
                codeWriter.StartBlock();
                codeWriter.WriteLine("return (object?)Results.Empty;");
                codeWriter.EndBlock();
            }
            codeWriter.WriteLine($"var result = await task;");
            codeWriter.WriteLine("return (object?)result;");
        }
        else
        {
            codeWriter.WriteLine($"return ValueTask.FromResult<object?>(handler({endpoint.EmitFilteredArgumentList()}));");
        }
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
            // The null suppression operator on the GetArgument(...) call here is required because we'll occassionally be
            // dealing with nullable types here. We could try to do fancy things to branch the logic here depending on
            // the nullability, but at the end of the day we are going to call GetArguments(...) - at runtime the nullability
            // suppression operator doesn't come into play - so its not worth worrying about.
            sb.Append($"ic.GetArgument<{EmitUnwrappedParameterType(endpoint.Parameters[i])}>({i})!");

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
            sb.Append(EmitUnwrappedParameterType(endpoint.Parameters[i]));

            if (i < endpoint.Parameters.Length - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }
}
