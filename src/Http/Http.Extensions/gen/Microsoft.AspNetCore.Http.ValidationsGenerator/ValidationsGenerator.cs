// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

[Generator]
public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
//         while (!System.Diagnostics.Debugger.IsAttached)
//         {
//             System.Threading.Thread.Sleep(1000);
// #pragma warning disable RS1035 // Do not use APIs banned for analyzers
//             System.Console.WriteLine($"Waiting for debugger to attach on {System.Diagnostics.Process.GetCurrentProcess().Id}...");
// #pragma warning restore RS1035 // Do not use APIs banned for analyzers
//         }
        // Resolve the symbols that will be required when making comparisons
        // in future steps.
        var requiredSymbols = context.CompilationProvider.Select(ExtractRequiredSymbols);

        // Find the builder.Services.AddValidation() call in the application.
        var addValidation = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: FindAddValidation,
            transform: TransformAddValidation
        );
        // Extract types that have been marked with [ValidatableType].
        var validatableTypesWithAttribute = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.AspNetCore.Http.Validation.ValidatableTypeAttribute",
            predicate: ShouldTransformSymbolWithAttribute,
            transform: TransformValidatableTypeWithAttribute
        );
        // Extract all minimal API endpoints in the application.
        var endpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: FindEndpoints,
                transform: TransformEndpoints)
            .Where(endpoint => endpoint is not null);
        // Extract validatable types from all endpoints.
        var validatableTypesFromEndpoints = endpoints
            .Combine(requiredSymbols)
            .Select(ExtractValidatableEndpoint);
        // Extract all validatable types encountered in the type graph.
        var validatableTypes = validatableTypesFromEndpoints
            .Concat(validatableTypesWithAttribute)
            .Distinct(ValidatableTypeComparer.Instance)
            .Collect();

        var emitInputs = addValidation
            .Combine(validatableTypes);

        // Emit the IValidatableInfo resolver injection and
        // ValidatableTypeInfo for all validatable types.
        context.RegisterSourceOutput(emitInputs, Emit);
    }
}
