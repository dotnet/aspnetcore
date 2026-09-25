// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040 // The framework implements this experimental contract.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiSchemaEvidenceResolver
{
    public static OpenApiSchemaEvidence? Resolve(
        JsonTypeInfo typeInfo,
        JsonConverter converter,
        InferredSchemaPurpose purpose,
        IReadOnlyList<IOpenApiSchemaEvidenceProvider> registeredProviders,
        Type? declaredType = null)
    {
        var context = new OpenApiSchemaEvidenceContext(
            declaredType ?? typeInfo.Type,
            typeInfo,
            converter,
            purpose switch
            {
                InferredSchemaPurpose.Input => OpenApiSchemaEvidencePurpose.Input,
                InferredSchemaPurpose.Output => OpenApiSchemaEvidencePurpose.Output,
                _ => OpenApiSchemaEvidencePurpose.Neutral,
            });
        OpenApiSchemaEvidence? result = null;
        List<string>? recognizingProviders = null;
        var evaluatedProviders = new HashSet<IOpenApiSchemaEvidenceProvider>(ReferenceEqualityComparer.Instance);

        for (var i = 0; i < registeredProviders.Count; i++)
        {
            Evaluate(registeredProviders[i]);
        }

        if (converter is IOpenApiSchemaEvidenceProvider converterProvider)
        {
            Evaluate(converterProvider);
        }

        if (recognizingProviders is { Count: > 1 })
        {
            throw new InvalidOperationException(Resources.FormatConflictingSchemaEvidenceProviders(
                typeInfo.Type,
                string.Join(", ", recognizingProviders)));
        }

        return result;

        void Evaluate(IOpenApiSchemaEvidenceProvider provider)
        {
            if (!evaluatedProviders.Add(provider))
            {
                return;
            }

#pragma warning disable ASP0040 // The framework invokes this experimental provider contract.
            if (provider.GetSchemaEvidence(context) is not { } evidence)
#pragma warning restore ASP0040
            {
                return;
            }

            result = evidence;
            recognizingProviders ??= [];
            recognizingProviders.Add(provider.GetType().FullName ?? provider.GetType().Name);
        }
    }
}
