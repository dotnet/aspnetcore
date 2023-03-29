// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandler.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandler.Emitters;

internal static class EndpointParameterEmitter
{
    /*
     * Iterates through all of the provided parameters
     * in a given endpoint and invokes the appropriate
     * emission sub-function based on the parameter type.
     */
    public static string EmitParameterPreparation(this IEnumerable<EndpointParameter> endpointParameters, EmitterContext emitterContext, int baseIndent = 0)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var parameterPreparationBuilder = new CodeWriter(stringWriter, baseIndent);
        var readFormEmitted = false;

        foreach (var parameter in endpointParameters)
        {
            switch (parameter.Source)
            {
                case EndpointParameterSource.SpecialType:
                    parameter.EmitSpecialParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.Query:
                case EndpointParameterSource.Header:
                    parameter.EmitQueryOrHeaderParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.Route:
                    parameter.EmitRouteParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.RouteOrQuery:
                    emitterContext.HasRouteOrQuery = true;
                    parameter.EmitRouteOrQueryParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.BindAsync:
                    emitterContext.HasBindAsync = true;
                    parameter.EmitBindAsyncPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.JsonBody:
                    parameter.EmitJsonBodyParameterPreparationString(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.FormBody:
                    parameter.EmitFormParameterPreparation(parameterPreparationBuilder, ref readFormEmitted);
                    break;
                case EndpointParameterSource.JsonBodyOrService:
                    parameter.EmitJsonBodyOrServiceParameterPreparationString(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.JsonBodyOrQuery:
                    parameter.EmitJsonBodyOrQueryParameterPreparationString(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.Service:
                    parameter.EmitServiceParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.AsParameters:
                    parameter.EmitAsParametersParameterPreparation(parameterPreparationBuilder, emitterContext);
                    break;
            }
        }

        return stringWriter.ToString();
    }

    /*
     * Emits resolvers that are invoked once at startup
     * for initializing how parameters that are ambiguous
     * at compile-time should be resolved.
     */
    public static void EmitRouteOrQueryResolver(this Endpoint endpoint, CodeWriter codeWriter)
    {
        foreach (var parameter in endpoint.Parameters)
        {
            ProcessParameter(parameter, codeWriter, endpoint);
            if (parameter is { Source: EndpointParameterSource.AsParameters, EndpointParameters: {} innerParameters })
            {
                foreach (var innerParameter in innerParameters)
                {
                    ProcessParameter(innerParameter, codeWriter, endpoint);
                }
            }
        }

        static void ProcessParameter(EndpointParameter parameter, CodeWriter codeWriter, Endpoint endpoint)
        {
            if (parameter.Source == EndpointParameterSource.RouteOrQuery)
            {
                var parameterName = parameter.SymbolName;
                codeWriter.Write($@"var {parameterName}_RouteOrQueryResolver = ");
                codeWriter.WriteLine($@"GeneratedRouteBuilderExtensionsCore.ResolveFromRouteOrQuery(""{parameterName}"", options?.RouteParameterNames);");
                endpoint.EmitterContext.HasRouteOrQuery = true;
            }
        }
    }

    public static void EmitJsonBodyOrServiceResolver(this Endpoint endpoint, CodeWriter codeWriter)
    {
        var serviceProviderEmitted = false;
        foreach (var parameter in endpoint.Parameters)
        {
            ProcessParameter(parameter, codeWriter, ref serviceProviderEmitted);
            if (parameter is { Source: EndpointParameterSource.AsParameters, EndpointParameters: {} innerParameters })
            {
                foreach (var innerParameter in innerParameters)
                {
                    ProcessParameter(innerParameter, codeWriter, ref serviceProviderEmitted);
                }
            }
        }

        static void ProcessParameter(EndpointParameter parameter, CodeWriter codeWriter, ref bool serviceProviderEmitted)
        {
            if (parameter.Source == EndpointParameterSource.JsonBodyOrService)
            {
                if (!serviceProviderEmitted)
                {
                    codeWriter.WriteLine("var serviceProviderIsService = serviceProvider?.GetService<IServiceProviderIsService>();");
                    serviceProviderEmitted = true;
                }
                codeWriter.Write($@"var {parameter.SymbolName}_JsonBodyOrServiceResolver = ");
                var shortParameterTypeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
                codeWriter.WriteLine($"ResolveJsonBodyOrService<{parameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)}>(logOrThrowExceptionHelper, {SymbolDisplay.FormatLiteral(shortParameterTypeName, true)}, {SymbolDisplay.FormatLiteral(parameter.SymbolName, true)}, serviceProviderIsService);");
            }
        }
    }

    /*
     * Emits parameter preparation for a parameter
     * of a given source.
     */
    private static void EmitSpecialParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
        => codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.AssigningCode};");

    private static void EmitQueryOrHeaderParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        var assigningCode = endpointParameter.Source is EndpointParameterSource.Header
            ? $"httpContext.Request.Headers[\"{endpointParameter.LookupName}\"]"
            : $"httpContext.Request.Query[\"{endpointParameter.LookupName}\"]";
        codeWriter.WriteLine($"var {endpointParameter.EmitAssigningCodeResult()} = {assigningCode};");

        // If we are not optional, then at this point we can just assign the string value to the handler argument,
        // otherwise we need to detect whether no value is provided and set the handler argument to null to
        // preserve consistency with RDF behavior. We don't want to emit the conditional block to avoid
        // compiler errors around null handling.
        if (endpointParameter.IsArray)
        {
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()}.ToArray();");
        }
        else if (endpointParameter.IsOptional)
        {
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()}.Count > 0 ? (string?){endpointParameter.EmitAssigningCodeResult()} : {endpointParameter.DefaultValue};");
        }
        else if (endpointParameter.IsStringValues)
        {
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()};");
        }
        else
        {
            codeWriter.WriteLine($"if (StringValues.IsNullOrEmpty({endpointParameter.EmitAssigningCodeResult()}))");
            codeWriter.StartBlock();
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.WriteLine($@"logOrThrowExceptionHelper.RequiredParameterNotProvided({SymbolDisplay.FormatLiteral(endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.ToMessageString(), true)});");
            codeWriter.EndBlock();
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = (string?){endpointParameter.EmitAssigningCodeResult()};");
        }

        endpointParameter.EmitParsingBlock(codeWriter);
    }

    private static void EmitFormParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter, ref bool readFormEmitted)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Invoke TryResolveFormAsync once per handler so that we can
        // avoid the blocking code-path that occurs when `httpContext.Request.Form`
        // is invoked.
        if (!readFormEmitted)
        {
            var shortParameterTypeName = endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
            var assigningCode = $"await GeneratedRouteBuilderExtensionsCore.TryResolveFormAsync(httpContext, logOrThrowExceptionHelper, {SymbolDisplay.FormatLiteral(shortParameterTypeName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)})";
            var resolveFormResult = $"{endpointParameter.SymbolName}_resolveFormResult";
            codeWriter.WriteLine($"var {resolveFormResult} = {assigningCode};");

            // Exit early if binding from the form has failed.
            codeWriter.WriteLine($"if (!{resolveFormResult}.Item1)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("return;");
            codeWriter.EndBlock();
            readFormEmitted = true;
        }

        codeWriter.WriteLine($"var {endpointParameter.EmitAssigningCodeResult()} = {endpointParameter.AssigningCode};");
        if (!endpointParameter.IsOptional && !endpointParameter.IsArray)
        {
            codeWriter.WriteLine($"if ({endpointParameter.EmitAssigningCodeResult()} == null)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.WriteLine($@"logOrThrowExceptionHelper.RequiredParameterNotProvided({SymbolDisplay.FormatLiteral(endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.ToMessageString(), true)});");
            codeWriter.EndBlock();
        }
        codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()};");
        endpointParameter.EmitParsingBlock(codeWriter);
    }

    private static void EmitParsingBlock(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        if (endpointParameter.IsArray && endpointParameter.IsParsable)
        {
            codeWriter.WriteLine($"{endpointParameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)} {endpointParameter.EmitHandlerArgument()} = new {endpointParameter.ElementType.ToDisplayString(EmitterConstants.DisplayFormat)}[{endpointParameter.EmitTempArgument()}.Length];");
            codeWriter.WriteLine($"for (var i = 0; i < {endpointParameter.EmitTempArgument()}.Length; i++)");
            codeWriter.StartBlock();
            codeWriter.WriteLine($"var element = {endpointParameter.EmitTempArgument()}[i];");
            endpointParameter.ParsingBlockEmitter(codeWriter, "element", "parsed_element");

            // In cases where we are dealing with an array of parsable nullables we need to substitute
            // empty strings for null values.
            if (endpointParameter.ElementType.NullableAnnotation == NullableAnnotation.Annotated)
            {
                codeWriter.WriteLine($"{endpointParameter.EmitHandlerArgument()}[i] = string.IsNullOrEmpty(element) ? null! : parsed_element!;");
            }
            else
            {
                codeWriter.WriteLine($"{endpointParameter.EmitHandlerArgument()}[i] = parsed_element!;");
            }
            codeWriter.EndBlock();
        }
        else if (endpointParameter.IsArray && !endpointParameter.IsParsable)
        {
            codeWriter.WriteLine($"{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitTempArgument()}!;");
        }
        else if (!endpointParameter.IsArray && endpointParameter.IsParsable)
        {
            endpointParameter.ParsingBlockEmitter(codeWriter, endpointParameter.EmitTempArgument(), endpointParameter.EmitParsedTempArgument());
            codeWriter.WriteLine($"{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitParsedTempArgument()}!;");
        }
        else // Not parsable, not an array.
        {
            codeWriter.WriteLine($"{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitTempArgument()}!;");
        }
    }

    private static void EmitRouteParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Throw an exception of if the route parameter name that was specific in the `FromRoute`
        // attribute or in the parameter name does not appear in the actual route.
        codeWriter.WriteLine($"""if (options?.RouteParameterNames?.Contains("{endpointParameter.LookupName}", StringComparer.OrdinalIgnoreCase) != true)""");
        codeWriter.StartBlock();
        codeWriter.WriteLine($$"""throw new InvalidOperationException($"'{{endpointParameter.LookupName}}' is not a route parameter.");""");
        codeWriter.EndBlock();

        var assigningCode = $"(string?)httpContext.Request.RouteValues[\"{endpointParameter.LookupName}\"]";
        codeWriter.WriteLine($"var {endpointParameter.EmitAssigningCodeResult()} = {assigningCode};");

        if (!endpointParameter.IsOptional)
        {
            codeWriter.WriteLine($"if ({endpointParameter.EmitAssigningCodeResult()} == null)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.WriteLine($@"logOrThrowExceptionHelper.RequiredParameterNotProvided({SymbolDisplay.FormatLiteral(endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.ToMessageString(), true)});");
            codeWriter.EndBlock();
        }

        codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = (string?){endpointParameter.EmitAssigningCodeResult()};");
        endpointParameter.EmitParsingBlock(codeWriter);
    }

    private static void EmitRouteOrQueryParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        var parameterName = endpointParameter.SymbolName;
        codeWriter.WriteLine($"var {endpointParameter.EmitAssigningCodeResult()} = {parameterName}_RouteOrQueryResolver(httpContext);");

        if (endpointParameter.IsArray)
        {
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()}.ToArray();");
        }
        else if (endpointParameter.IsOptional)
        {
            // For non-string parameters, the TryParse logic takes care of setting the default value fallback.
            // Strings don't undergo the TryParse treatment so we set the default value here.
            var fallback = endpointParameter.Type.SpecialType == SpecialType.System_String ? endpointParameter.DefaultValue : "null";
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()}.Count > 0 ? (string?){endpointParameter.EmitAssigningCodeResult()} : {fallback};");
        }
        else
        {
            codeWriter.WriteLine($"if ({endpointParameter.EmitAssigningCodeResult()} is StringValues {{ Count: 0 }})");
            codeWriter.StartBlock();
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.WriteLine($@"logOrThrowExceptionHelper.RequiredParameterNotProvided({SymbolDisplay.FormatLiteral(endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.ToMessageString(), true)});");
            codeWriter.EndBlock();
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = (string?){endpointParameter.EmitAssigningCodeResult()};");
        }

        endpointParameter.EmitParsingBlock(codeWriter);
    }

    private static void EmitJsonBodyParameterPreparationString(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        // Preamble for diagnostics purposes.
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Invoke TryResolveBody method to parse JSON and set
        // status codes on exceptions.
        var shortParameterTypeName = endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        var assigningCode = $"await GeneratedRouteBuilderExtensionsCore.TryResolveBodyAsync<{endpointParameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)}>(httpContext, logOrThrowExceptionHelper, {(endpointParameter.IsOptional ? "true" : "false")}, {SymbolDisplay.FormatLiteral(shortParameterTypeName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)})";
        var resolveBodyResult = $"{endpointParameter.SymbolName}_resolveBodyResult";
        codeWriter.WriteLine($"var {resolveBodyResult} = {assigningCode};");
        codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {resolveBodyResult}.Item2;");

        // If binding from the JSON body fails, we exit early. Don't
        // set the status code here because assume it has been set by the
        // TryResolveBody method.
        codeWriter.WriteLine($"if (!{resolveBodyResult}.Item1)");
        codeWriter.StartBlock();
        codeWriter.WriteLine("return;");
        codeWriter.EndBlock();
    }

    private static void EmitJsonBodyOrServiceParameterPreparationString(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        // Preamble for diagnostics purposes.
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Invoke ResolveJsonBodyOrService method to resolve the
        // type from DI if it exists. Otherwise, resolve the parameter
        // as a body parameter.
        var assigningCode = $"await {endpointParameter.SymbolName}_JsonBodyOrServiceResolver(httpContext, {(endpointParameter.IsOptional ? "true" : "false")})";
        var resolveJsonBodyOrServiceResult = $"{endpointParameter.SymbolName}_resolveJsonBodyOrServiceResult";
        codeWriter.WriteLine($"var {resolveJsonBodyOrServiceResult} = {assigningCode};");
        codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {resolveJsonBodyOrServiceResult}.Item2;");

        // If binding from the JSON body fails, ResolveJsonBodyOrService
        // will return `false` and we will need to exit early.
        codeWriter.WriteLine($"if (!{resolveJsonBodyOrServiceResult}.Item1)");
        codeWriter.StartBlock();
        codeWriter.WriteLine("return;");
        codeWriter.EndBlock();
    }

    private static void EmitJsonBodyOrQueryParameterPreparationString(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        // Preamble for diagnostics purposes.
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Declare handler variable up front.
        codeWriter.WriteLine($"{endpointParameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)} {endpointParameter.EmitHandlerArgument()} = null!;");
        codeWriter.WriteLine("if (options.DisableInferBodyFromParameters)");
        codeWriter.StartBlock();
        codeWriter.WriteLine($"""var {endpointParameter.EmitAssigningCodeResult()} = httpContext.Request.Query["{endpointParameter.LookupName}"];""");
        codeWriter.WriteLine($"""{endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitAssigningCodeResult()}!;""");
        codeWriter.EndBlock();
        codeWriter.WriteLine("else");
        codeWriter.StartBlock();

        // This code is adapted from the EmitJsonBodyParameterPreparationString method with some modifications
        // because the handler argument is emitted before the containing if block (which makes it awkward to
        // simply reuse that emission code) - opted for duplication (with tweaks) over complexity.
        var shortParameterTypeName = endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        var assigningCode = $"await GeneratedRouteBuilderExtensionsCore.TryResolveBodyAsync<{endpointParameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)}>(httpContext, logOrThrowExceptionHelper, {(endpointParameter.IsOptional ? "true" : "false")}, {SymbolDisplay.FormatLiteral(shortParameterTypeName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)})";
        var resolveBodyResult = $"{endpointParameter.SymbolName}_resolveBodyResult";
        codeWriter.WriteLine($"var {resolveBodyResult} = {assigningCode};");
        codeWriter.WriteLine($"{endpointParameter.EmitHandlerArgument()} = {resolveBodyResult}.Item2!;");

        // If binding from the JSON body fails, we exit early. Don't
        // set the status code here because assume it has been set by the
        // TryResolveBody method.
        codeWriter.WriteLine($"if (!{resolveBodyResult}.Item1)");
        codeWriter.StartBlock();
        codeWriter.WriteLine("return;");
        codeWriter.EndBlock();

        codeWriter.EndBlock();
    }

    private static void EmitBindAsyncPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        // Invoke the `BindAsync` method on an interface if it is the target receiver.
        var receiverType = endpointParameter.BindableMethodSymbol?.ReceiverType is { TypeKind: TypeKind.Interface } targetType
            ? targetType
            : endpointParameter.Type;
        var bindMethodReceiverType = receiverType?.UnwrapTypeSymbol(unwrapNullable: true);
        var bindMethodReceiverTypeString = bindMethodReceiverType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var unwrappedType = endpointParameter.Type.UnwrapTypeSymbol(unwrapNullable: true);
        var unwrappedTypeString = unwrappedType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var resolveParameterInfo = endpointParameter.IsProperty
            ? endpointParameter.PropertyAsParameterInfoConstruction
            : $"parameters[{endpointParameter.Ordinal}]";

        switch (endpointParameter.BindMethod)
        {
            case BindabilityMethod.IBindableFromHttpContext:
                codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = await BindAsync<{unwrappedTypeString}>(httpContext, {resolveParameterInfo});");
                break;
            case BindabilityMethod.BindAsyncWithParameter:
                codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = await {bindMethodReceiverTypeString}.BindAsync(httpContext, {resolveParameterInfo});");
                break;
            case BindabilityMethod.BindAsync:
                codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = await {bindMethodReceiverTypeString}.BindAsync(httpContext);");
                break;
            default:
                throw new NotImplementedException($"Unreachable! Unexpected {nameof(BindabilityMethod)}: {endpointParameter.BindMethod}");
        }

        // TODO: Generate more compact code if the type is a reference type and/or the BindAsync return nullability matches the handler parameter type.
        if (endpointParameter.IsOptional)
        {
            codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = ({unwrappedTypeString}?){endpointParameter.EmitTempArgument()};");
        }
        else
        {
            codeWriter.WriteLine($"{unwrappedTypeString} {endpointParameter.EmitHandlerArgument()};");
            codeWriter.WriteLine($"if ((object?){endpointParameter.EmitTempArgument()} == null)");
            codeWriter.StartBlock();
            codeWriter.WriteLine($@"logOrThrowExceptionHelper.RequiredParameterNotProvided({SymbolDisplay.FormatLiteral(endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), true)}, {SymbolDisplay.FormatLiteral(endpointParameter.SymbolName, true)}, {SymbolDisplay.FormatLiteral(endpointParameter.ToMessageString(), true)});");
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.WriteLine($"{endpointParameter.EmitHandlerArgument()} = default!;");
            codeWriter.EndBlock();
            codeWriter.WriteLine("else");
            codeWriter.StartBlock();
            codeWriter.WriteLine($"{endpointParameter.EmitHandlerArgument()} = ({unwrappedTypeString}){endpointParameter.EmitTempArgument()};");
            codeWriter.EndBlock();
        }
    }

    private static void EmitServiceParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Requiredness checks for services are handled by the distinction
        // between GetRequiredService and GetService in the assigningCode.
        // Unlike other scenarios, this will result in an exception being thrown
        // at runtime.
        var assigningCode = endpointParameter.IsOptional ?
            $"httpContext.RequestServices.GetService<{endpointParameter.Type}>();" :
            $"httpContext.RequestServices.GetRequiredService<{endpointParameter.Type}>()";
        codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {assigningCode};");
    }

    private static void EmitAsParametersParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter, EmitterContext emitterContext)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());
        codeWriter.WriteLine(endpointParameter.EndpointParameters?.EmitParameterPreparation(baseIndent: codeWriter.Indent, emitterContext: emitterContext));
        codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.AssigningCode};");
    }

    /*
     * Emitters for metadata-related aspects of parameters, such as invoking
     * IEndpointParameterMetadataProviders, annotating metadata for form
     * and JSON content types, and so on.
     */
    public static void EmitCallsToMetadataProvidersForParameters(this Endpoint endpoint, CodeWriter codeWriter)
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
            else
            {
                ProcessParameter(parameter, codeWriter);
            }
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

    private static void EmitFormAcceptsMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        var hasFormFiles = endpoint.Parameters.Any(p => p.IsFormFile);

        if (hasFormFiles)
        {
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(new GeneratedAcceptsMetadata(contentTypes: GeneratedMetadataConstants.FormFileContentType));");
        }
        else
        {
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(new GeneratedAcceptsMetadata(contentTypes: GeneratedMetadataConstants.FormContentType));");
        }
    }

    private static void EmitJsonAcceptsMetadata(this Endpoint endpoint, CodeWriter codeWriter)
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
            else if (parameter.Source == EndpointParameterSource.JsonBodyOrService)
            {
                potentialImplicitBodyParameters.Add(parameter);
            }
        }

        if (explicitBodyParameter != null)
        {
            codeWriter.WriteLine($$"""options.EndpointBuilder.Metadata.Add(new GeneratedAcceptsMetadata(type: typeof({{explicitBodyParameter.Type.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability)}}), isOptional: {{(explicitBodyParameter.IsOptional ? "true" : "false")}}, contentTypes: GeneratedMetadataConstants.JsonContentType));""");
        }
        else if (potentialImplicitBodyParameters.Count > 0)
        {
            codeWriter.WriteLine("var serviceProvider = options.ServiceProvider ?? options.EndpointBuilder.ApplicationServices;");
            codeWriter.WriteLine($"var serviceProviderIsService = serviceProvider.GetRequiredService<IServiceProviderIsService>();");

            codeWriter.WriteLine("var jsonBodyOrServiceTypeTuples = new (bool, Type)[] {");
            codeWriter.Indent++;
            foreach (var parameter in potentialImplicitBodyParameters)
            {
                codeWriter.WriteLine($$"""({{(parameter.IsOptional ? "true" : "false")}}, typeof({{parameter.Type.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability)}})),""");
            }
            codeWriter.Indent--;
            codeWriter.WriteLine("};");
            codeWriter.WriteLine("foreach (var (isOptional, type) in jsonBodyOrServiceTypeTuples)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("if (!serviceProviderIsService.IsService(type))");
            codeWriter.StartBlock();
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(new GeneratedAcceptsMetadata(type: type, isOptional: isOptional, contentTypes: GeneratedMetadataConstants.JsonContentType));");
            codeWriter.WriteLine("break;");
            codeWriter.EndBlock();
            codeWriter.EndBlock();
        }
        else
        {
            codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new GeneratedAcceptsMetadata(contentTypes: GeneratedMetadataConstants.JsonContentType));");
        }
    }

    public static void EmitAcceptsMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        var hasJsonBody = endpoint.EmitterContext.HasJsonBody || endpoint.EmitterContext.HasJsonBodyOrService;

        if (endpoint.EmitterContext.HasFormBody)
        {
            endpoint.EmitFormAcceptsMetadata(codeWriter);
        }
        else if (hasJsonBody)
        {
            endpoint.EmitJsonAcceptsMetadata(codeWriter);
        }
    }

    /*
     * Helpers that are used for parameter preparation code.
     */
    private static string EmitParameterDiagnosticComment(this EndpointParameter endpointParameter) => $"// Endpoint Parameter: {endpointParameter.SymbolName} (Type = {endpointParameter.Type}, IsOptional = {endpointParameter.IsOptional}, IsParsable = {endpointParameter.IsParsable}, IsArray = {endpointParameter.IsArray}, Source = {endpointParameter.Source})";

    private static string EmitTempArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.SymbolName}_temp";

    private static string EmitParsedTempArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.SymbolName}_parsed_temp";
    private static string EmitAssigningCodeResult(this EndpointParameter endpointParameter) => $"{endpointParameter.SymbolName}_raw";
    public static void EmitLoggingPreamble(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.EmitterContext.RequiresLoggingHelper)
        {
            codeWriter.WriteLine("var logOrThrowExceptionHelper = new LogOrThrowExceptionHelper(serviceProvider, options);");
        }
    }
}
