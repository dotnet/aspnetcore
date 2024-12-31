// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find the app.Conventions.WithValidation to call to indicate the
        // user has opted in to validation for the minimal APIs.
        var withValidation = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: FindWithValidation,
            transform: TransformWithValidation
        );
        // Extract all minimal API endpoints in the application.
        var endpoints = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: FindEndpoints,
            transform: TransformEndpoints);
        // Resolve the symbols that will be required when making comparisons
        // in future steps.
        var requiredSymbols = context.CompilationProvider.Select(ExtractRequireSymbols);
        // Extract all validatable endpoints encountered in the type graph.
        var validatableEndpoints = endpoints
            .Combine(requiredSymbols)
            .Select(ExtractValidatableEndpoint);
        // Extract all validatable types encountered in the type graph.
        var validatableTypes = validatableEndpoints
            .SelectMany((endpoint, ct) => endpoint.ValidatableTypes)
            .Distinct(ValidatableTypeComparer.Instance)
            .Collect();

        // Generate emitted code for subtypes, interceptions, and filters.
        var typeValidations = validatableTypes
            .Select(EmitTypeValidations);
        var withValidationInterceptions = withValidation
            .Where(location => location is not null)
            .Select(EmitWithValidationInterception);
        var validationsFilters = validatableEndpoints.Select(EmitEndpointValidationFilter).Collect();

        var validations = withValidationInterceptions
            .Combine(typeValidations)
            .Combine(validationsFilters);

        context.RegisterSourceOutput(validations, EmitValidationsFile);
    }
}
