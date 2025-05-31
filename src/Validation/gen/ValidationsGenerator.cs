// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

[Generator(LanguageNames.CSharp)]
public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find the builder.Services.AddValidation() call in the application.
        var addValidation = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: FindAddValidation,
            transform: TransformAddValidation
        );
        // Extract types that have been marked with [ValidatableType].
        var validatableTypesWithAttribute = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.Extensions.Validation.ValidatableTypeAttribute",
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
            .Select(ExtractValidatableEndpoint);
        // Join all validatable types encountered in the type graph.
        var validatableTypes = validatableTypesWithAttribute
            .Concat(validatableTypesFromEndpoints)
            .Distinct(ValidatableTypeComparer.Instance)
            .Collect();

        var emitInputs = addValidation
            .Combine(validatableTypes);

        // Emit the IValidatableInfo resolver injection and
        // ValidatableTypeInfo for all validatable types.
        context.RegisterSourceOutput(emitInputs, Emit);
    }
}
