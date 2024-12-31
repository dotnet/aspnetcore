// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator
{
    internal static string EmitEndpointValidationFilters(ImmutableArray<string> filterDeclarations)
    {
        var writer = new StringWriter();
        var code = new CodeWriter(writer, baseIndent: 1);
        code.WriteLine("file static class ValidationsFilters");
        code.StartBlock();
        code.WriteLine("public static readonly global::System.Collections.Generic.Dictionary<EndpointKey, global::System.Func<global::Microsoft.AspNetCore.Http.EndpointFilterInvocationContext, global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails?>> Filters = new()");
        code.WriteLine("{");
        code.Indent--;
        foreach (var filter in filterDeclarations)
        {
            code.WriteLine(filter);
        }
        code.Indent++;
        code.WriteLine("};");
        code.Indent--;
        code.WriteLine("}");
        return writer.ToString();
    }

    internal string EmitEndpointValidationFilter(ValidatableEndpoint endpoint, CancellationToken cancellationToken)
    {
        var writer = new StringWriter();
        var code = new CodeWriter(writer, baseIndent: 2);
        code.WriteLine($@"{{ {endpoint.EndpointKey}, context => ");
        code.Indent++;
        code.Indent++;
        code.StartBlock();
        code.WriteLine("ValidationProblemBuilder resultBuilder = new();");
        var validationResultEmitted = false;
        foreach (var parameter in endpoint.Parameters)
        {
            var parameterType = parameter.OriginalType.ToDisplayString();
            var parameterIndex = parameter.Index;
            if (parameter.Attributes.Any() && !validationResultEmitted)
            {
                code.WriteLine("global::System.ComponentModel.DataAnnotations.ValidationResult? validationResult = null;");
                validationResultEmitted = true;
            }
            code.WriteLine($"var value{parameterIndex} = context.GetArgument<{parameterType}>({parameterIndex});");
            foreach (var attribute in parameter.Attributes)
            {
                code.WriteLine(EmitValidationAttribute(attribute));
            }
            if (parameter.Attributes.Any())
            {
                foreach (var attribute in parameter.Attributes)
                {
                    code.WriteLine($@"validationResult = {attribute.Name}.GetValidationResult(value{parameterIndex}, new global::System.ComponentModel.DataAnnotations.ValidationContext(value{parameterIndex}) {{ DisplayName = ""{parameter.DisplayName}"" }});");
                    code.WriteLine("if (validationResult is not null)");
                    code.StartBlock();
                    code.WriteLine(@$"resultBuilder.WithError(""{parameter.Name}"", validationResult.ErrorMessage);");
                    code.EndBlock();
                }
            }
            if (parameter.IsEnumerable && parameter.HasValidatableType)
            {
                code.WriteLine($"var value{parameterIndex}Index = 0;");
                code.WriteLine($"foreach (var item in value{parameterIndex} ?? [])");
                code.StartBlock();
                code.WriteLine($"var itemValidationResult = ValidationTypes.Validate(item, validationContext);");
                code.WriteLine($"if (itemValidationResult is not null)");
                code.StartBlock();
                code.WriteLine($"foreach (var error in itemValidationResult.Errors)");
                code.StartBlock();
                code.WriteLine($@"resultBuilder.WithErrors($""value{parameterIndex}[{{ value{parameterIndex}Index }}].{{error.Key}}"", error.Value);");
                code.EndBlock();
                code.WriteLine($"value{parameterIndex}Index++;");
                code.EndBlock();
                code.EndBlock();
            }
            else if (parameter.HasValidatableType)
            {
                code.WriteLine($"var typeValidationResult = ValidationTypes.Validate(value{parameterIndex}, serviceProvider: context.HttpContext.RequestServices);");
                code.WriteLine("if (typeValidationResult is not null)");
                code.StartBlock();
                code.WriteLine("foreach (var error in typeValidationResult.Errors)");
                code.StartBlock();
                code.WriteLine("resultBuilder.WithErrors(error.Key, error.Value);");
                code.EndBlock();
                code.EndBlock();
            }
        }
        code.WriteLine("return resultBuilder.HasValue() ? resultBuilder.Build() : null;");
        code.EndBlock();
        code.EndBlockWithComma();
        return writer.ToString();
    }
}
