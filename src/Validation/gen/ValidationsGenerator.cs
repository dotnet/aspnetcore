// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
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
        var frameworkValidatableTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.Extensions.Validation.ValidatableTypeAttribute",
            predicate: ShouldTransformSymbolWithAttribute,
            transform: TransformValidatableTypeWithAttribute
        );

        // Extract types that have been marked with generated [ValidatableType].
        var generatedValidatableTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.Extensions.Validation.Embedded.ValidatableTypeAttribute",
            predicate: ShouldTransformSymbolWithAttribute,
            transform: TransformValidatableTypeWithAttribute
        );

        // Combine both sources of validatable types
        var validatableTypesWithAttribute = frameworkValidatableTypes.Concat(generatedValidatableTypes);

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
}
