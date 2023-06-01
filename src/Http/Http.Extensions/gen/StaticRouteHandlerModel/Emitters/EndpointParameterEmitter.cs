// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;

internal static class EndpointParameterEmitter
{
    internal static void EmitSpecialParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
        => codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.AssigningCode};");

    internal static void EmitQueryOrHeaderParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitFormParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter, ref bool readFormEmitted)
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

    internal static void EmitParsingBlock(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitRouteParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitRouteOrQueryParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitJsonBodyParameterPreparationString(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitJsonBodyOrServiceParameterPreparationString(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitJsonBodyOrQueryParameterPreparationString(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitBindAsyncPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitServiceParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
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

    internal static void EmitAsParametersParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter, EmitterContext emitterContext)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());
        codeWriter.WriteLine(endpointParameter.EndpointParameters?.EmitParameterPreparation(baseIndent: codeWriter.Indent, emitterContext: emitterContext));
        codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.AssigningCode};");
    }

    private static string EmitParameterDiagnosticComment(this EndpointParameter endpointParameter) => $"// Endpoint Parameter: {endpointParameter.SymbolName} (Type = {endpointParameter.Type}, IsOptional = {endpointParameter.IsOptional}, IsParsable = {endpointParameter.IsParsable}, IsArray = {endpointParameter.IsArray}, Source = {endpointParameter.Source})";

    private static string EmitTempArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.SymbolName}_temp";

    private static string EmitParsedTempArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.SymbolName}_parsed_temp";
    private static string EmitAssigningCodeResult(this EndpointParameter endpointParameter) => $"{endpointParameter.SymbolName}_raw";
}
