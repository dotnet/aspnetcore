// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// A default implementation of <see cref="ICompositeMetadataDetailsProvider"/>.
/// </summary>
#pragma warning disable CA1852 // Seal internal types
internal class DefaultCompositeMetadataDetailsProvider : ICompositeMetadataDetailsProvider
#pragma warning restore CA1852 // Seal internal types
{
    private readonly IEnumerable<IMetadataDetailsProvider> _providers;

    /// <summary>
    /// Creates a new <see cref="DefaultCompositeMetadataDetailsProvider"/>.
    /// </summary>
    /// <param name="providers">The set of <see cref="IMetadataDetailsProvider"/> instances.</param>
    public DefaultCompositeMetadataDetailsProvider(IEnumerable<IMetadataDetailsProvider> providers)
    {
        _providers = providers;
    }

    /// <inheritdoc />
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var provider in _providers.OfType<IBindingMetadataProvider>())
        {
            provider.CreateBindingMetadata(context);
        }
    }

    /// <inheritdoc />
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var provider in _providers.OfType<IDisplayMetadataProvider>())
        {
            provider.CreateDisplayMetadata(context);
        }
    }

    /// <inheritdoc />
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var provider in _providers.OfType<IValidationMetadataProvider>())
        {
            provider.CreateValidationMetadata(context);
        }
    }
}
