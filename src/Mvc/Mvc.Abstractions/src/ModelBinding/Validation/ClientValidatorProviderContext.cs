// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// A context for <see cref="IClientModelValidatorProvider"/>.
/// </summary>
public class ClientValidatorProviderContext
{
    /// <summary>
    /// Creates a new <see cref="ClientValidatorProviderContext"/>.
    /// </summary>
    /// <param name="modelMetadata">The <see cref="ModelBinding.ModelMetadata"/> for the model being validated.
    /// </param>
    /// <param name="items">The list of <see cref="ClientValidatorItem"/>s.</param>
    public ClientValidatorProviderContext(ModelMetadata modelMetadata, IList<ClientValidatorItem> items)
    {
        ModelMetadata = modelMetadata;
        Results = items;
    }

    /// <summary>
    /// Gets the <see cref="ModelBinding.ModelMetadata"/>.
    /// </summary>
    public ModelMetadata ModelMetadata { get; }

    /// <summary>
    /// Gets the validator metadata.
    /// </summary>
    /// <remarks>
    /// This property provides convenience access to <see cref="ModelMetadata.ValidatorMetadata"/>.
    /// </remarks>
    public IReadOnlyList<object> ValidatorMetadata => ModelMetadata.ValidatorMetadata;

    /// <summary>
    /// Gets the list of <see cref="ClientValidatorItem"/> instances. <see cref="IClientModelValidatorProvider"/>
    /// instances should add the appropriate <see cref="ClientValidatorItem.Validator"/> properties when
    /// <see cref="IClientModelValidatorProvider.CreateValidators(ClientValidatorProviderContext)"/>
    /// is called.
    /// </summary>
    public IList<ClientValidatorItem> Results { get; }
}
