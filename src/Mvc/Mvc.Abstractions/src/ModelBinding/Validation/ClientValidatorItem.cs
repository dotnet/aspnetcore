// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Used to associate validators with <see cref="ValidatorMetadata"/> instances
/// as part of <see cref="ClientValidatorProviderContext"/>. An <see cref="IClientModelValidator"/> should
/// inspect <see cref="ClientValidatorProviderContext.Results"/> and set <see cref="Validator"/> and
/// <see cref="IsReusable"/> as appropriate.
/// </summary>
public class ClientValidatorItem
{
    /// <summary>
    /// Creates a new <see cref="ClientValidatorItem"/>.
    /// </summary>
    public ClientValidatorItem()
    {
    }

    /// <summary>
    /// Creates a new <see cref="ClientValidatorItem"/>.
    /// </summary>
    /// <param name="validatorMetadata">The <see cref="ValidatorMetadata"/>.</param>
    public ClientValidatorItem(object? validatorMetadata)
    {
        ValidatorMetadata = validatorMetadata;
    }

    /// <summary>
    /// Gets the metadata associated with the <see cref="Validator"/>.
    /// </summary>
    public object? ValidatorMetadata { get; }

    /// <summary>
    /// Gets or sets the <see cref="IClientModelValidator"/>.
    /// </summary>
    public IClientModelValidator? Validator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not <see cref="Validator"/> can be reused across requests.
    /// </summary>
    public bool IsReusable { get; set; }
}
