// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

internal sealed class CompareAttributeAdapter : AttributeAdapterBase<CompareAttribute>
{
    private readonly string _otherProperty;

    public CompareAttributeAdapter(CompareAttribute attribute, IStringLocalizer? stringLocalizer)
        : base(new CompareAttributeWrapper(attribute), stringLocalizer)
    {
        _otherProperty = "*." + attribute.OtherProperty;
    }

    public override void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-equalto", GetErrorMessage(context));
        MergeAttribute(context.Attributes, "data-val-equalto-other", _otherProperty);
    }

    /// <inheritdoc />
    public override string GetErrorMessage(ModelValidationContextBase validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        var displayName = validationContext.ModelMetadata.GetDisplayName();
        var otherPropertyDisplayName = CompareAttributeWrapper.GetOtherPropertyDisplayName(
            validationContext,
            Attribute);

        ((CompareAttributeWrapper)Attribute).ValidationContext = validationContext;

        return GetErrorMessage(validationContext.ModelMetadata, displayName, otherPropertyDisplayName);
    }

    // TODO: This entire class is needed because System.ComponentModel.DataAnnotations.CompareAttribute doesn't
    // populate OtherPropertyDisplayName until you call FormatErrorMessage.
    private sealed class CompareAttributeWrapper : CompareAttribute
    {
        public ModelValidationContextBase ValidationContext { get; set; } = default!;

        public CompareAttributeWrapper(CompareAttribute attribute)
            : base(attribute.OtherProperty)
        {
            // Copy settable properties from wrapped attribute. Don't reset default message accessor (set as
            // CompareAttribute constructor calls ValidationAttribute constructor) when all properties are null to
            // preserve default error message. Reset the message accessor when just ErrorMessageResourceType is
            // non-null to ensure correct InvalidOperationException.
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) ||
                !string.IsNullOrEmpty(attribute.ErrorMessageResourceName) ||
                attribute.ErrorMessageResourceType != null)
            {
                ErrorMessage = attribute.ErrorMessage;
                ErrorMessageResourceName = attribute.ErrorMessageResourceName;
                ErrorMessageResourceType = attribute.ErrorMessageResourceType;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            var displayName = ValidationContext.ModelMetadata.GetDisplayName();
            return string.Format(CultureInfo.CurrentCulture,
                                 ErrorMessageString,
                                 displayName,
                                 GetOtherPropertyDisplayName(ValidationContext, this));
        }

        public static string GetOtherPropertyDisplayName(
            ModelValidationContextBase validationContext,
            CompareAttribute attribute)
        {
            // The System.ComponentModel.DataAnnotations.CompareAttribute doesn't populate the
            // OtherPropertyDisplayName until after IsValid() is called. Therefore, at the time we get
            // the error message for client validation, the display name is not populated and won't be used.
            var otherPropertyDisplayName = attribute.OtherPropertyDisplayName;
            if (otherPropertyDisplayName == null && validationContext.ModelMetadata.ContainerType != null)
            {
                var otherProperty = validationContext.MetadataProvider.GetMetadataForProperty(
                    validationContext.ModelMetadata.ContainerType,
                    attribute.OtherProperty);
                if (otherProperty != null)
                {
                    return otherProperty.GetDisplayName();
                }
            }

            return attribute.OtherProperty;
        }
    }
}
