// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointParameterEmitter
{
    internal static string EmitSpecialParameterPreparation(this EndpointParameter endpointParameter)
    {
        return $"""
                        var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.AssigningCode};
""";
    }

    internal static string EmitQueryOrHeaderParameterPreparation(this EndpointParameter endpointParameter)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

        var assigningCode = endpointParameter.Source is EndpointParameterSource.Header
            ? $"httpContext.Request.Headers[\"{endpointParameter.Name}\"]"
            : $"httpContext.Request.Query[\"{endpointParameter.Name}\"]";
        builder.AppendLine($$"""
                        var {{endpointParameter.EmitAssigningCodeResult()}} = {{assigningCode}};
""");

        // If we are not optional, then at this point we can just assign the string value to the handler argument,
        // otherwise we need to detect whether no value is provided and set the handler argument to null to
        // preserve consistency with RDF behavior. We don't want to emit the conditional block to avoid
        // compiler errors around null handling.
        if (endpointParameter.IsOptional)
        {
            builder.AppendLine($$"""
                        var {{endpointParameter.Name}}_temp = {{endpointParameter.EmitAssigningCodeResult()}}.Count > 0 ? {{endpointParameter.Name}}_raw.ToString() : null;
""");
        }
        else
        {
            builder.AppendLine($$"""
                        if (StringValues.IsNullOrEmpty({{endpointParameter.EmitAssigningCodeResult()}}))
                        {
                            wasParamCheckFailure = true;
                        }
                        var {{endpointParameter.Name}}_temp = {{endpointParameter.EmitAssigningCodeResult()}}.ToString();
""");
        }

        builder.Append(endpointParameter.EmitParsingBlock());

        return builder.ToString();
    }

    internal static string EmitParsingBlock(this EndpointParameter endpointParameter)
    {
        var builder = new StringBuilder();

        if (endpointParameter.IsParsable)
        {
            var parsingBlock = endpointParameter.ParsingBlockEmitter($"{endpointParameter.Name}_temp", $"{endpointParameter.Name}_parsed_temp");
            builder.AppendLine($$"""
{{parsingBlock}}
                        {{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{endpointParameter.EmitHandlerArgument()}} = {{endpointParameter.Name}}_parsed_temp!;
""");

        }
        else
        {
            builder.AppendLine($$"""
                        {{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{endpointParameter.EmitHandlerArgument()}} = {{endpointParameter.Name}}_temp!;
""");

        }

        return builder.ToString();
    }

        internal static string EmitRouteParameterPreparation(this EndpointParameter endpointParameter)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

            // Throw an exception of if the route parameter name that was specific in the `FromRoute`
            // attribute or in the parameter name does not appear in the actual route.
            builder.AppendLine($$"""
                        if (options?.RouteParameterNames?.Contains("{{endpointParameter.Name}}", StringComparer.OrdinalIgnoreCase) != true)
                        {
                            throw new InvalidOperationException($"'{{endpointParameter.Name}}' is not a route parameter.");
                        }
""");

            var assigningCode = $"httpContext.Request.RouteValues[\"{endpointParameter.Name}\"]?.ToString()";
            builder.AppendLine($$"""
                        var {{endpointParameter.EmitAssigningCodeResult()}} = {{assigningCode}};
""");

            if (!endpointParameter.IsOptional)
            {
                builder.AppendLine($$"""
                        if ({{endpointParameter.EmitAssigningCodeResult()}} == null)
                        {
                            wasParamCheckFailure = true;
                        }
""");
            }
            builder.AppendLine($"""
                        var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitAssigningCodeResult()};
""");

            return builder.ToString();
        }

        internal static string EmitRouteOrQueryParameterPreparation(this EndpointParameter endpointParameter)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

            var parameterName = endpointParameter.Name;
            var assigningCode = $@"options?.RouteParameterNames?.Contains(""{parameterName}"", StringComparer.OrdinalIgnoreCase) == true";
            assigningCode += $@"? new StringValues(httpContext.Request.RouteValues[$""{parameterName}""]?.ToString())";
            assigningCode += $@": httpContext.Request.Query[$""{parameterName}""];";

            builder.AppendLine($$"""
                        var {{endpointParameter.EmitAssigningCodeResult()}} = {{assigningCode}};
""");

            if (!endpointParameter.IsOptional)
            {
                builder.AppendLine($$"""
                        if ({{endpointParameter.EmitAssigningCodeResult()}} is StringValues { Count: 0 })
                        {
                            wasParamCheckFailure = true;
                        }
""");
            }

            builder.AppendLine($"""
                        var {endpointParameter.EmitHandlerArgument()} = {endpointParameter.EmitAssigningCodeResult()};
""");

            return builder.ToString();
        }

        internal static string EmitJsonBodyParameterPreparationString(this EndpointParameter endpointParameter)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

            var assigningCode = $"await GeneratedRouteBuilderExtensionsCore.TryResolveBody<{endpointParameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)}>(httpContext, {(endpointParameter.IsOptional ? "true" : "false")})";
            builder.AppendLine($$"""
                        var (isSuccessful, {{endpointParameter.EmitHandlerArgument()}}) = {{assigningCode}};
""");

            // If binding from the JSON body fails, we exit early. Don't
            // set the status code here because assume it has been set by the
            // TryResolveBody method.
            builder.AppendLine("""
                        if (!isSuccessful)
                        {
                            return;
                        }
""");

            return builder.ToString();
        }

        internal static string EmitServiceParameterPreparation(this EndpointParameter endpointParameter)
        {
            var builder = new StringBuilder();

            // Preamble for diagnostics purposes.
            builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

            // Requiredness checks for services are handled by the distinction
            // between GetRequiredService and GetService in the assigningCode.
            // Unlike other scenarios, this will result in an exception being thrown
            // at runtime.
            var assigningCode = endpointParameter.IsOptional ?
                $"httpContext.RequestServices.GetService<{endpointParameter.Type}>();" :
                $"httpContext.RequestServices.GetRequiredService<{endpointParameter.Type}>()";

            builder.AppendLine($$"""
                        var {{endpointParameter.EmitHandlerArgument()}} = {{assigningCode}};
""");

            return builder.ToString();
        }

        private static string EmitParameterDiagnosticComment(this EndpointParameter endpointParameter) => $"// Endpoint Parameter: {endpointParameter.Name} (Type = {endpointParameter.Type}, IsOptional = {endpointParameter.IsOptional}, IsParsable = {endpointParameter.IsParsable}, Source = {endpointParameter.Source})";
        private static string EmitHandlerArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.Name}_local";
        private static string EmitAssigningCodeResult(this EndpointParameter endpointParameter) => $"{endpointParameter.Name}_raw";

        public static string EmitArgument(this EndpointParameter endpointParameter) => endpointParameter.Source switch
        {
            EndpointParameterSource.JsonBody or EndpointParameterSource.Route or EndpointParameterSource.RouteOrQuery => endpointParameter.IsOptional ? endpointParameter.EmitHandlerArgument() : $"{endpointParameter.EmitHandlerArgument()}!",
            EndpointParameterSource.Unknown => throw new Exception("Unreachable!"),
            _ => endpointParameter.EmitHandlerArgument()
        };
    }
