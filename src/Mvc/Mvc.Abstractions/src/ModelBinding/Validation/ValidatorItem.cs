// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Used to associate validators with <see cref="ValidatorMetadata"/> instances
/// as part of <see cref="ModelValidatorProviderContext"/>. An <see cref="IModelValidator"/> should
/// inspect <see cref="ModelValidatorProviderContext.Results"/> and set <see cref="Validator"/> and
/// <see cref="IsReusable"/> as appropriate.
/// </summary>
public class ValidatorItem
{
    /// <summary>
    /// Creates a new <see cref="ValidatorItem"/>.
    /// </summary>
    public ValidatorItem()
    {
    }

    /// <summary>
    /// Creates a new <see cref="ValidatorItem"/>.
    /// </summary>
    /// <param name="validatorMetadata">The <see cref="ValidatorMetadata"/>.</param>
    public ValidatorItem(object validatorMetadata)
    {
        ValidatorMetadata = validatorMetadata;
    }

    /// <summary>
    /// Gets the metadata associated with the <see cref="Validator"/>.
    /// </summary>
    public object ValidatorMetadata { get; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="IModelValidator"/>.
    /// </summary>
    public IModelValidator? Validator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not <see cref="Validator"/> can be reused across requests.
    /// </summary>
    public bool IsReusable { get; set; }
}
