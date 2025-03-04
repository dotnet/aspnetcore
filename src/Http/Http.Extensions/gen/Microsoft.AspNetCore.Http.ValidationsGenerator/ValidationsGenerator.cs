// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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
        // Extract all validatable endpoints encountered in the type graph.
        var validatableEndpoints = endpoints
            .Combine(requiredSymbols)
            .Select(ExtractValidatableEndpoint);
        // Extract all validatable types encountered in the type graph.
        var validatableTypes = validatableEndpoints
            .SelectMany((endpoint, ct) => endpoint.ValidatableTypes)
            .Concat(validatableTypesWithAttribute)
            .Distinct(ValidatableTypeComparer.Instance)
            .Collect();

        var emitInputs = addValidation
            .Combine(validatableTypes);

        // Emit ValidatableTypeInfo for all validatable types.
        context.RegisterSourceOutput(emitInputs, Emit);
    }
}
