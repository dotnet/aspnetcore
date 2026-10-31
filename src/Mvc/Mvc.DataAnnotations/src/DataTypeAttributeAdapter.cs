// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// A validation adapter that is used to map <see cref="DataTypeAttribute"/>'s to a single client side validation
/// rule.
/// </summary>
internal sealed class DataTypeAttributeAdapter : AttributeAdapterBase<DataTypeAttribute>
{
    public DataTypeAttributeAdapter(DataTypeAttribute attribute, string ruleName, IStringLocalizer? stringLocalizer)
        : base(attribute, stringLocalizer)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleName);

        RuleName = ruleName;
    }

    public string RuleName { get; }

    public override void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, RuleName, GetErrorMessage(context));
    }

    /// <inheritdoc/>
    public override string GetErrorMessage(ModelValidationContextBase validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        return GetErrorMessage(
            validationContext.ModelMetadata,
            validationContext.ModelMetadata.GetDisplayName(),
            Attribute.GetDataTypeName());
    }
}
