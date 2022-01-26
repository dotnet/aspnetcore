// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// An <see cref="NullableEmptyBodyBindingMetadataProvider"/> which configures <see cref="ModelMetadata.IsEmptyBodyAllowed"/> to
/// <c>true</c> for Nullable or with default value types.
/// </summary>
public class NullableEmptyBodyBindingMetadataProvider : IBindingMetadataProvider
{
    private readonly NullabilityInfoContext _nullabilityContext = new();

    /// <inheritdoc />
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // No-op if the metadata is not for the Parameter Metadatakind
        if (context.Key.MetadataKind == ModelMetadataKind.Parameter
            && !context.BindingMetadata.IsEmptyBodyAllowed.HasValue
            && IsOptional(context.Key))
        {
            context.BindingMetadata.IsEmptyBodyAllowed = true;
        }
    }

    // internal for testing
    internal bool IsOptional(ModelMetadataIdentity identity)
    {
        // If the parameter has a default value we don't need to
        // work with the NullabilityInfoContext
        if (identity.ParameterInfo!.HasDefaultValue)
        {
            return true;
        }

        var nullability = _nullabilityContext.Create(identity.ParameterInfo!);
        return nullability.ReadState == NullabilityState.Nullable;
    }
}
