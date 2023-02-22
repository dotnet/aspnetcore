// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointParameterEmitter
{
    internal static string EmitSpecialParameterPreparation(this EndpointParameter endpointParameter)
    {
        return $"""
                        var {endpointParameter.Name}_local = {endpointParameter.AssigningCode};
""";
    }

    internal static string EmitQueryParameterPreparation(this EndpointParameter endpointParameter)
    {
        var builder = new StringBuilder();

        // Preamble for diagnostics purposes.
        builder.AppendLine($"""
                        {endpointParameter.EmitParameterDiagnosticComment()}
""");

        // Grab raw input from HttpContext.
        builder.AppendLine($$"""
                        var {{endpointParameter.Name}}_raw = {{endpointParameter.AssigningCode}};
""");

        // If we are not optional, then at this point we can just assign the string value to the handler argument,
        // otherwise we need to detect whether no value is provided and set the handler argument to null to
        // preserve consistency with RDF behavior. We don't want to emit the conditional block to avoid
        // compiler errors around null handling.
        if (endpointParameter.IsOptional)
        {
            builder.AppendLine($$"""
                        var {{endpointParameter.HandlerArgument}} = {{endpointParameter.Name}}_raw.Count > 0 ? {{endpointParameter.Name}}_raw.ToString() : null;
""");
        }
        else
        {
            builder.AppendLine($$"""
                        if (StringValues.IsNullOrEmpty({{endpointParameter.Name}}_raw))
                        {
                            wasParamCheckFailure = true;
                        }
                        var {{endpointParameter.HandlerArgument}} = {{endpointParameter.Name}}_raw.ToString();
""");
        }

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
                        var (isSuccessful, {{endpointParameter.Name}}_local) = {{endpointParameter.AssigningCode}};
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
