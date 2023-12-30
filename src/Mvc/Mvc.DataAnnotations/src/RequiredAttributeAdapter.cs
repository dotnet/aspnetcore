// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// <see cref="AttributeAdapterBase{TAttribute}"/> for <see cref="RequiredAttribute"/>.
/// </summary>
public sealed class RequiredAttributeAdapter : AttributeAdapterBase<RequiredAttribute>
{
    /// <summary>
    /// Initializes a new instance of <see cref="RequiredAttributeAdapter"/>.
    /// </summary>
    /// <param name="attribute">The <see cref="RequiredAttribute"/>.</param>
    /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
    public RequiredAttributeAdapter(RequiredAttribute attribute, IStringLocalizer? stringLocalizer)
        : base(attribute, stringLocalizer)
    {
    }

    /// <inheritdoc />
    public override void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-required", GetErrorMessage(context));
    }

    /// <inheritdoc />
    public override string GetErrorMessage(ModelValidationContextBase validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        return GetErrorMessage(validationContext.ModelMetadata, validationContext.ModelMetadata.GetDisplayName());
    }
}
