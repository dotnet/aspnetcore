// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Provides <see cref="BindingMetadata"/> for a <see cref="DefaultModelMetadata"/>.
/// </summary>
public class BindingSourceMetadataProvider : IBindingMetadataProvider
{
    /// <summary>
    /// Creates a new <see cref="BindingSourceMetadataProvider"/> for the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The <see cref="Type"/>. The provider sets <see cref="BindingSource"/> of the given <see cref="Type"/> or
    /// anything assignable to the given <see cref="Type"/>.
    /// </param>
    /// <param name="bindingSource">
    /// The <see cref="BindingSource"/> to assign to the given <paramref name="type"/>.
    /// </param>
    public BindingSourceMetadataProvider(Type type, BindingSource? bindingSource)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type = type;
        BindingSource = bindingSource;
    }

    /// <summary>
    /// The <see cref="Type"/>. The provider sets <see cref="BindingSource"/> of the given <see cref="Type"/> or
    /// anything assignable to the given <see cref="Type"/>.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The <see cref="BindingSource"/> to assign to the Type.
    /// </summary>
    public BindingSource? BindingSource { get; }

    /// <inheritdoc />
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Type.IsAssignableFrom(context.Key.ModelType))
        {
            context.BindingMetadata.BindingSource = BindingSource;
        }
    }
}
