// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// An <see cref="IBindingMetadataProvider"/> which configures <see cref="ModelMetadata.IsBindingAllowed"/> to
/// <c>false</c> for matching types.
/// </summary>
public class ExcludeBindingMetadataProvider : IBindingMetadataProvider
{
    private readonly Type _type;

    /// <summary>
    /// Creates a new <see cref="ExcludeBindingMetadataProvider"/> for the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The <see cref="Type"/>. All properties with this <see cref="Type"/> will have
    /// <see cref="ModelMetadata.IsBindingAllowed"/> set to <c>false</c>.
    /// </param>
    public ExcludeBindingMetadataProvider(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _type = type;
    }

    /// <inheritdoc />
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // No-op if the metadata is not for the target type
        if (!_type.IsAssignableFrom(context.Key.ModelType))
        {
            return;
        }

        context.BindingMetadata.IsBindingAllowed = false;
    }
}
