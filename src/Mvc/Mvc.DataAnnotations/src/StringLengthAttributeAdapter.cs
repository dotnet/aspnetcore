// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

internal sealed class StringLengthAttributeAdapter : AttributeAdapterBase<StringLengthAttribute>
{
    private readonly string _max;
    private readonly string _min;

    public StringLengthAttributeAdapter(StringLengthAttribute attribute, IStringLocalizer? stringLocalizer)
        : base(attribute, stringLocalizer)
    {
        _max = Attribute.MaximumLength.ToString(CultureInfo.InvariantCulture);
        _min = Attribute.MinimumLength.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-length", GetErrorMessage(context));

        if (Attribute.MaximumLength != int.MaxValue)
        {
            MergeAttribute(context.Attributes, "data-val-length-max", _max);
        }

        if (Attribute.MinimumLength != 0)
        {
            MergeAttribute(context.Attributes, "data-val-length-min", _min);
        }
    }

    /// <inheritdoc />
    public override string GetErrorMessage(ModelValidationContextBase validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        return GetErrorMessage(
            validationContext.ModelMetadata,
            validationContext.ModelMetadata.GetDisplayName(),
            Attribute.MaximumLength,
            Attribute.MinimumLength);
    }
}
