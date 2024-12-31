// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Threading;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator
{
    internal static string EmitTypeValidations(ImmutableArray<ValidatableType> validatableTypes, CancellationToken cancellationToken)
    {
        var writer = new StringWriter();
        var code = new CodeWriter(writer, baseIndent: 1);
        code.WriteLine("file static class ValidationTypes");
        code.StartBlock();
        code.WriteLine("public static global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails? Validate<T>(T value, global::System.ComponentModel.DataAnnotations.ValidationContext? validationContext = null, IServiceProvider? serviceProvider = null, int currentDepth = 0, bool skipRecurse = false) => null;");
        if (validatableTypes.Length == 0)
        {
            code.EndBlock();
            return writer.ToString();
        }
        code.WriteLine();
        foreach (var type in validatableTypes)
        {
            var needsValidationResult = true;
            foreach (var member in type.Members)
            {
                foreach (var attribute in member.Attributes)
                {
                    code.WriteLine(EmitValidationAttribute(attribute));
                }
            }
            code.WriteLine();
            code.WriteLine($"public static global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails? Validate({type.Name}? value, global::System.ComponentModel.DataAnnotations.ValidationContext? validationContext = null, IServiceProvider? serviceProvider = null, int currentDepth = 0, bool skipRecurse = false)");
            code.StartBlock();
            code.WriteLine("var maxDepth = ((global::Microsoft.Extensions.Options.IOptions<global::Microsoft.AspNetCore.Http.Json.JsonOptions>)(serviceProvider ?? validationContext).GetService(typeof(global::Microsoft.Extensions.Options.IOptions<global::Microsoft.AspNetCore.Http.Json.JsonOptions>))).Value?.SerializerOptions.MaxDepth;");
            code.WriteLine("if (currentDepth > (maxDepth == null || maxDepth == 0 ? 64 : maxDepth))");
            code.StartBlock();
            code.WriteLine("return null;");
            code.EndBlock();
            code.WriteLine("ValidationProblemBuilder resultBuilder = new();");
            code.WriteLine($"if (value != null)");
            code.StartBlock();
            foreach (var subTypeName in type.ValidatableSubTypeNames)
            {
                code.WriteLine($"if (value is {subTypeName} subType{subTypeName})");
                code.StartBlock();
                code.WriteLine($"var subType{subTypeName}ValidationResult = ValidationTypes.Validate(({subTypeName})subType{subTypeName}, validationContext, serviceProvider, currentDepth: currentDepth + 1, skipRecurse);");
                code.WriteLine($"if (subType{subTypeName}ValidationResult is not null)");
                code.StartBlock();
                code.WriteLine($"foreach (var error in subType{subTypeName}ValidationResult.Errors)");
                code.StartBlock();
                code.WriteLine("resultBuilder.WithErrors(error.Key, error.Value);");
                code.EndBlock();
                code.EndBlock();
                code.EndBlock();
            }
            foreach (var derivedTypeName in type.ValidatableDerivedTypeNames)
            {
                code.WriteLine($"if (!skipRecurse && value is {derivedTypeName} derivedType{derivedTypeName})");
                code.StartBlock();
                code.WriteLine($"var derivedType{derivedTypeName}ValidationResult = ValidationTypes.Validate(derivedType{derivedTypeName}, validationContext, serviceProvider, currentDepth: currentDepth + 1, skipRecurse: true);");
                code.WriteLine($"if (derivedType{derivedTypeName}ValidationResult is not null)");
                code.StartBlock();
                code.WriteLine($"foreach (var error in derivedType{derivedTypeName}ValidationResult.Errors)");
                code.StartBlock();
                code.WriteLine("resultBuilder.WithErrors(error.Key, error.Value);");
                code.EndBlock();
                code.EndBlock();
                code.WriteLine("return resultBuilder.HasValue() ? resultBuilder.Build() : null;");
                code.EndBlock();
            }
            code.WriteLine("validationContext ??= new(value, serviceProvider: serviceProvider, items: null);");
            foreach (var member in type.Members)
            {
                code.WriteLine($@"validationContext.DisplayName = ""{member.DisplayName}"";");
                code.WriteLine($@"validationContext.MemberName = ""{member.Name}"";");
                if (member.IsEnumerable && member.HasValidatableType)
                {
                    code.WriteLine($"var {member.Name}Index = 0;");
                    code.WriteLine($"foreach (var item in value?.{member.Name} ?? [])");
                    code.StartBlock();
                    code.WriteLine($"var itemValidationResult = ValidationTypes.Validate(item, validationContext, serviceProvider, currentDepth, skipRecurse);");
                    code.WriteLine($"if (itemValidationResult is not null)");
                    code.StartBlock();
                    code.WriteLine($"foreach (var error in itemValidationResult.Errors)");
                    code.StartBlock();
                    code.WriteLine($@"resultBuilder.WithErrors($""{member.Name}[{{ {member.Name}Index }}].{{error.Key}}"", error.Value);");
                    code.EndBlock();
                    code.WriteLine($"{member.Name}Index++;");
                    code.EndBlock();
                    code.EndBlock();
                }
                else if (member.HasValidatableType)
                {
                    code.WriteLine($"var type{member.Name}ValidationResult = ValidationTypes.Validate(value?.{member.Name}, validationContext, serviceProvider, currentDepth: currentDepth + 1, skipRecurse);");
                    code.WriteLine($"if (type{member.Name}ValidationResult is not null)");
                    code.StartBlock();
                    code.WriteLine($"foreach (var error in type{member.Name}ValidationResult.Errors)");
                    code.StartBlock();
                    code.WriteLine($@"resultBuilder.WithErrors($""{member.Name}.{{error.Key}}"", error.Value);");
                    code.EndBlock();
                    code.EndBlock();
                }
                foreach (var attribute in member.Attributes)
                {
                    if (needsValidationResult)
                    {
                        code.WriteLine("global::System.ComponentModel.DataAnnotations.ValidationResult? validationResult;");
                        needsValidationResult = false;
                    }
                    code.WriteLine($@"validationResult = {attribute.Name}.GetValidationResult(value?.{member.Name}, validationContext);");
                    code.WriteLine("if (validationResult is global::System.ComponentModel.DataAnnotations.ValidationResult { ErrorMessage: not null })");
                    code.StartBlock();
                    code.WriteLine($@"resultBuilder.WithError(""{member.Name}"", validationResult.ErrorMessage);");
                    code.EndBlock();
                }
            }
            if (type.IsIValidatableObject)
            {
                code.WriteLine("if (!resultBuilder.HasValue())");
                code.StartBlock();
                code.WriteLine("validationContext = new(value, serviceProvider: serviceProvider, items: null);");
                code.WriteLine("foreach (var validatableValidationResult in value.Validate(validationContext))");
                code.StartBlock();
                code.WriteLine("if (validatableValidationResult is global::System.ComponentModel.DataAnnotations.ValidationResult { ErrorMessage: not null })");
                code.StartBlock();
                code.WriteLine($@"resultBuilder.WithError(validatableValidationResult.MemberNames.FirstOrDefault() ?? ""{type.Name}"", validatableValidationResult.ErrorMessage);");
                code.EndBlock();
                code.EndBlock();
                code.EndBlock();
            }
            code.EndBlock();
            code.WriteLine("return resultBuilder.HasValue() ? resultBuilder.Build() : null;");
            code.EndBlock();
        }
        code.EndBlock();
        return writer.ToString();
    }
}
