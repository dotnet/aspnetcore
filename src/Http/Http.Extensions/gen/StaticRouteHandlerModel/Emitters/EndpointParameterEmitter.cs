// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointParameterEmitter
{
    internal static string EmitSpecialParameterPreparation(this EndpointParameter endpointParameter)
    {
        return $"""
                        var {endpointParameter.HandlerArgument} = {endpointParameter.AssigningCode};
""";
    }

    internal static string EmitQueryOrHeaderParameterPreparation(this EndpointParameter endpointParameter)
    {
        var builder = new StringBuilder();

        // Preamble for diagnostics purposes.
        builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

        // Grab raw input from HttpContext.
        builder.AppendLine($$"""
                        var {{endpointParameter.AssigningCodeResult}} = {{endpointParameter.AssigningCode}};
""");

        // If we are not optional, then at this point we can just assign the string value to the handler argument,
        // otherwise we need to detect whether no value is provided and set the handler argument to null to
        // preserve consistency with RDF behavior. We don't want to emit the conditional block to avoid
        // compiler errors around null handling.
        if (endpointParameter.IsOptional)
        {
            builder.AppendLine($$"""
                        var {{endpointParameter.HandlerArgument}} = {{endpointParameter.AssigningCodeResult}}.Count > 0 ? {{endpointParameter.AssigningCodeResult}}.ToString() : null;
""");
        }
        else
        {
            builder.AppendLine($$"""
                        if (StringValues.IsNullOrEmpty({{endpointParameter.AssigningCodeResult}}))
                        {
                            wasParamCheckFailure = true;
                        }
                        var {{endpointParameter.HandlerArgument}} = {{endpointParameter.AssigningCodeResult}}.ToString();
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

        builder.AppendLine($$"""
                        var {{endpointParameter.AssigningCodeResult}} = {{endpointParameter.AssigningCode}};
""");

        if (!endpointParameter.IsOptional)
        {
            builder.AppendLine($$"""
                        if ({{endpointParameter.AssigningCodeResult}} == null)
                        {
                            wasParamCheckFailure = true;
                        }
""");
        }
        builder.AppendLine($"""
                        var {endpointParameter.HandlerArgument} = {endpointParameter.AssigningCodeResult};
""");

        return builder.ToString();
    }

    internal static string EmitRouteOrQueryParameterPreparation(this EndpointParameter endpointParameter)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

        builder.AppendLine($$"""
                        var {{endpointParameter.AssigningCodeResult}} = {{endpointParameter.AssigningCode}};
""");

        if (!endpointParameter.IsOptional)
        {
            builder.AppendLine($$"""
                        if ({{endpointParameter.AssigningCodeResult}} is StringValues { Count: 0 })
                        {
                            wasParamCheckFailure = true;
                        }
""");
        }

        builder.AppendLine($"""
                        var {endpointParameter.HandlerArgument} = {endpointParameter.AssigningCodeResult};
""");

        return builder.ToString();
    }

    internal static string EmitJsonBodyParameterPreparationString(this EndpointParameter endpointParameter)
    {
        var builder = new StringBuilder();

        // Preamble for diagnostics purposes.
        builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

        // Grab raw input from HttpContext.
        builder.AppendLine($$"""
                        var (isSuccessful, {{endpointParameter.HandlerArgument}}) = {{endpointParameter.AssigningCode}};
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
        // between GetRequiredService and GetService in the AssigningCode.
        builder.AppendLine($$"""
                        var {{endpointParameter.HandlerArgument}} = {{endpointParameter.AssigningCode}};
""");

        return builder.ToString();
    }

    private static string EmitParameterDiagnosticComment(this EndpointParameter endpointParameter) =>
        $"// Endpoint Parameter: {endpointParameter.Name} (Type = {endpointParameter.Type}, IsOptional = {endpointParameter.IsOptional}, Source = {endpointParameter.Source})";
}
