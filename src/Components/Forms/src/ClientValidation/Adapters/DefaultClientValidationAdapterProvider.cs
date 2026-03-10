// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Default implementation that maps built-in <see cref="ValidationAttribute"/>
/// types to <see cref="IClientValidationAdapter"/> instances.
/// Falls back to registered <see cref="IClientValidationAdapterProvider"/>
/// services for custom attributes.
/// </summary>
internal sealed class DefaultClientValidationAdapterProvider : IClientValidationAdapterProvider
{
    private readonly IEnumerable<IClientValidationAdapterProvider> _customProviders;

    public DefaultClientValidationAdapterProvider(
        IEnumerable<IClientValidationAdapterProvider> customProviders)
    {
        _customProviders = customProviders;
    }

    public IClientValidationAdapter? GetAdapter(ValidationAttribute attribute)
    {
        var adapter = attribute switch
        {
            RequiredAttribute => new RequiredClientAdapter(),
            StringLengthAttribute a => new StringLengthClientAdapter(a),
            RangeAttribute a => new RangeClientAdapter(a),
            MinLengthAttribute a => new MinLengthClientAdapter(a),
            MaxLengthAttribute a => new MaxLengthClientAdapter(a),
            RegularExpressionAttribute a => new RegexClientAdapter(a),
            EmailAddressAttribute => new DataTypeClientAdapter("data-val-email"),
            UrlAttribute => new DataTypeClientAdapter("data-val-url"),
            CreditCardAttribute => new DataTypeClientAdapter("data-val-creditcard"),
            PhoneAttribute => new DataTypeClientAdapter("data-val-phone"),
            CompareAttribute a => new CompareClientAdapter(a),
            _ => (IClientValidationAdapter?)null
        };

        if (adapter is not null)
        {
            return adapter;
        }

        foreach (var provider in _customProviders)
        {
            adapter = provider.GetAdapter(attribute);
            if (adapter is not null)
            {
                return adapter;
            }
        }

        return null;
    }
}
