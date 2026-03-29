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
        // Parse generator configuration from syntax (e.g., IncludeInternalTypes() call)
        var configuration = ParseGeneratorConfiguration(context);

        // Find the builder.Services.AddValidation() call in the application.
        var addValidation = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: FindAddValidation,
            transform: TransformAddValidation
        );

        // Extract types that have been marked with framework [ValidatableType].
        var frameworkValidatableTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.Extensions.Validation.ValidatableTypeAttribute",
            predicate: ShouldTransformSymbolWithAttribute,
            transform: ExtractValidatableTypeWithAttributeSymbol
        ).Combine(configuration).Select(RetriveValidatableTypes);

        // Extract  types that have been marked with generated [ValidatableType].
        var generatedValidatableTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.Extensions.Validation.Embedded.ValidatableTypeAttribute",
            predicate: ShouldTransformSymbolWithAttribute,
            transform: ExtractValidatableTypeWithAttributeSymbol
        ).Combine(configuration).Select(RetriveValidatableTypes);

        // Combine both sources of validatable type symbols
        var validatableTypesWithAttribute = frameworkValidatableTypes.Concat(generatedValidatableTypes);

        // Extract all minimal API endpoints in the application.
        var endpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: FindEndpoints,
                transform: TransformEndpoints)
            .Where(endpoint => endpoint is not null);

        var endpointWithConfiguration = endpoints.Combine(configuration);

        // Extract validatable types from all endpoints.
        var validatableTypesFromEndpoints = endpointWithConfiguration.Select(ExtractValidatableEndpoint);

        // Join all validatable types encountered in the type graph.
        var allValidatableTypesProviders = validatableTypesFromEndpoints
            .Concat(validatableTypesWithAttribute);

        var validatableTypes = allValidatableTypesProviders
            .Distinct(ValidatableTypeComparer.Instance)
            .Collect();

        var emitInputs = addValidation
            .Combine(validatableTypes);

        // Emit the IValidatableInfo resolver injection and
        // ValidatableTypeInfo for all validatable types.
        context.RegisterSourceOutput(emitInputs, (context, emitInputs) =>
            Emit(context, (emitInputs.Left, emitInputs.Right)));
    }

    private IncrementalValueProvider<GeneratorConfiguration> ParseGeneratorConfiguration(IncrementalGeneratorInitializationContext context)
    {
        // Evaluates if IncludeInternalTypes was set to true
        var includeInternalTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: FindAddValidationOptionsConfiguration,
            transform: TransformIncludeInternalTypes
        ).Collect().Select((values, _) => values.Any(v => v));

        return includeInternalTypes.Select((include, _) =>
            include ? GeneratorConfiguration.IncludeInternalTypes() : GeneratorConfiguration.Default);
    }
}
