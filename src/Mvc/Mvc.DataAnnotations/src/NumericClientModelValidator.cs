// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// An implementation of <see cref="IClientModelValidator"/> that provides the rule for validating
/// numeric types.
/// </summary>
internal sealed class NumericClientModelValidator : IClientModelValidator
{
    /// <inheritdoc />
    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-number", GetErrorMessage(context.ModelMetadata));
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }

    private static string GetErrorMessage(ModelMetadata modelMetadata)
    {
        ArgumentNullException.ThrowIfNull(modelMetadata);

        var messageProvider = modelMetadata.ModelBindingMessageProvider;
        var name = modelMetadata.DisplayName ?? modelMetadata.Name;
        if (name == null)
        {
            return messageProvider.NonPropertyValueMustBeANumberAccessor();
        }

        return messageProvider.ValueMustBeANumberAccessor(name);
    }
}
