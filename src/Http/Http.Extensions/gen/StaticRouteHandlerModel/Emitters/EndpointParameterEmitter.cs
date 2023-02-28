// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointParameterEmitter
{
    internal static void EmitSpecialParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
        => codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.AssigningCode};");

    internal static void EmitQueryOrHeaderParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        var assigningCode = endpointParameter.Source is EndpointParameterSource.Header
            ? $"httpContext.Request.Headers[\"{endpointParameter.Name}\"]"
            : $"httpContext.Request.Query[\"{endpointParameter.Name}\"]";
        codeWriter.WriteLine($"var {endpointParameter.EmitAssigningCodeResult()} = {assigningCode};");

        // If we are not optional, then at this point we can just assign the string value to the handler argument,
        // otherwise we need to detect whether no value is provided and set the handler argument to null to
        // preserve consistency with RDF behavior. We don't want to emit the conditional block to avoid
        // compiler errors around null handling.
        if (endpointParameter.IsOptional)
        {
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()}.Count > 0 ? {endpointParameter.EmitAssigningCodeResult()}.ToString() : null;");
        }
        else
        {
            codeWriter.WriteLine($"if (StringValues.IsNullOrEmpty({endpointParameter.EmitAssigningCodeResult()}))");
            codeWriter.StartBlock();
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.EndBlock();
            codeWriter.WriteLine($"var {endpointParameter.EmitTempArgument()} = {endpointParameter.EmitAssigningCodeResult()}.ToString();");
        }

        endpointParameter.EmitParsingBlock(codeWriter);
    }

    internal static void EmitParsingBlock(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        if (endpointParameter.IsParsable)
        {
            var parsingBlock = endpointParameter.ParsingBlockEmitter(endpointParameter.EmitTempArgument(), endpointParameter.EmitParsedTempArgument());
            codeWriter.WriteLine(parsingBlock);
            codeWriter.WriteLine($"{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitParsedTempArgument()}!;");
        }
        else
        {
            codeWriter.WriteLine($"{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitTempArgument()}!;");
        }
    }

    internal static void EmitRouteParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Throw an exception of if the route parameter name that was specific in the `FromRoute`
        // attribute or in the parameter name does not appear in the actual route.
        codeWriter.WriteLine($"""if (options?.RouteParameterNames?.Contains("{endpointParameter.Name}", StringComparer.OrdinalIgnoreCase) != true)""");
        codeWriter.StartBlock();
        codeWriter.WriteLine($$"""throw new InvalidOperationException($"'{{endpointParameter.Name}}' is not a route parameter.");""");
        codeWriter.EndBlock();

        var assigningCode = $"httpContext.Request.RouteValues[\"{endpointParameter.Name}\"]?.ToString()";
        codeWriter.WriteLine($"var {endpointParameter.EmitAssigningCodeResult()} = {assigningCode};");

        if (!endpointParameter.IsOptional)
        {
            codeWriter.WriteLine($"if ({endpointParameter.EmitAssigningCodeResult()} == null)");
            codeWriter.StartBlock();
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.EndBlock();
        }

        codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitAssigningCodeResult()};");
    }

    internal static void EmitRouteOrQueryParameterPreparation(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        var parameterName = endpointParameter.Name;
        codeWriter.Write($"var {endpointParameter.EmitAssigningCodeResult()} = ");
        codeWriter.WriteLine($@"options?.RouteParameterNames?.Contains(""{parameterName}"", StringComparer.OrdinalIgnoreCase) == true");
        codeWriter.Indent++;
        codeWriter.WriteLine($@"? new StringValues(httpContext.Request.RouteValues[$""{parameterName}""]?.ToString())");
        codeWriter.WriteLine($@": httpContext.Request.Query[$""{parameterName}""];");
        codeWriter.Indent--;

        if (!endpointParameter.IsOptional)
        {
            codeWriter.WriteLine($"if ({endpointParameter.EmitAssigningCodeResult()} is StringValues {{ Count: 0 }})");
            codeWriter.StartBlock();
            codeWriter.WriteLine("wasParamCheckFailure = true;");
            codeWriter.EndBlock();
        }

        codeWriter.WriteLine($"var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitAssigningCodeResult()};");
    }

    internal static void EmitJsonBodyParameterPreparationString(this EndpointParameter endpointParameter, CodeWriter codeWriter)
    {
        // Preamble for diagnostics purposes.
        codeWriter.WriteLine(endpointParameter.EmitParameterDiagnosticComment());

        // Invoke TryResolveBody method to parse JSON and set
        // status codes on exceptions.
        var assigningCode = $"await GeneratedRouteBuilderExtensionsCore.TryResolveBody<{endpointParameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)}>(httpContext, {(endpointParameter.IsOptional ? "true" : "false")})";
        codeWriter.WriteLine($"var (isSuccessful, {endpointParameter.EmitHandlerArgument()}) = {assigningCode};");

        // If binding from the JSON body fails, we exit early. Don't
        // set the status code here because assume it has been set by the
        // TryResolveBody method.
        codeWriter.WriteLine("if (!isSuccessful)");
        codeWriter.StartBlock();
        codeWriter.WriteLine("return;");
        codeWriter.EndBlock();
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

    private static string EmitParameterDiagnosticComment(this EndpointParameter endpointParameter) => $"// Endpoint Parameter: {endpointParameter.Name} (Type = {endpointParameter.Type}, IsOptional = {endpointParameter.IsOptional}, IsParsable = {endpointParameter.IsParsable}, Source = {endpointParameter.Source})";
    private static string EmitHandlerArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.Name}_local";
    private static string EmitTempArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.Name}_temp";

    private static string EmitParsedTempArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.Name}_parsed_temp";
    private static string EmitAssigningCodeResult(this EndpointParameter endpointParameter) => $"{endpointParameter.Name}_raw";

    public static string EmitArgument(this EndpointParameter endpointParameter) => endpointParameter.Source switch
    {
        EndpointParameterSource.JsonBody or EndpointParameterSource.Route or EndpointParameterSource.RouteOrQuery => endpointParameter.IsOptional ? endpointParameter.EmitHandlerArgument() : $"{endpointParameter.EmitHandlerArgument()}!",
        EndpointParameterSource.Unknown => throw new Exception("Unreachable!"),
        _ => endpointParameter.EmitHandlerArgument()
    };
}
