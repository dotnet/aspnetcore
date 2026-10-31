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

        // Extract types that have been marked with framework [ValidatableType].
        var validatableTypesWithAttribute = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.Extensions.Validation.ValidatableTypeAttribute",
            predicate: ShouldTransformSymbolWithAttribute,
            transform: TransformValidatableTypeWithAttribute
        );

        // Extract all minimal API endpoints in the application.
        // Extract validatable types from all endpoints.
        var validatableTypesFromEndpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: FindEndpoints,
                transform: TransformEndpoints)
            .Where(endpoint => !endpoint.IsDefault);

        // Join all validatable types encountered in the type graph.
        var allValidatableTypesProviders = validatableTypesFromEndpoints
            .Concat(validatableTypesWithAttribute);

        var validatableTypes = allValidatableTypesProviders
            .Distinct(ValidatableTypeComparer.Instance)
            .Collect();

        // Collect all AddValidation call sites to avoid emitting duplicate hint names
        // when AddValidation() is called multiple times in the same project.
        var addValidationLocations = addValidation.Collect();

        var emitInputs = addValidationLocations
            .Combine(validatableTypes);

        // Emit the IValidatableInfo resolver injection and
        // ValidatableTypeInfo for all validatable types.
        context.RegisterSourceOutput(emitInputs, (context, emitInputs) =>
            Emit(context, (emitInputs.Left, emitInputs.Right)));
    }
}
