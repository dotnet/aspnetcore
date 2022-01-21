// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class ValidationAttributeAdapterProviderTest
{
    private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider =
        new ValidationAttributeAdapterProvider();

    public static TheoryData<ValidationAttribute, Type> DataAnnotationAdapters
    {
        get
        {
            return new TheoryData<ValidationAttribute, Type>
                {
                    {
                        new RegularExpressionAttribute("abc"),
                        typeof(RegularExpressionAttributeAdapter)
                    },
                    {
                        new MaxLengthAttribute(),
                        typeof(MaxLengthAttributeAdapter)
                    },
                    {
                        new MinLengthAttribute(1),
                        typeof(MinLengthAttributeAdapter)
                    },
                    {
                        new RangeAttribute(1, 100),
                        typeof(RangeAttributeAdapter)
                    },
                    {
                        new StringLengthAttribute(6),
                        typeof(StringLengthAttributeAdapter)
                    },
                    {
                        new RequiredAttribute(),
                        typeof(RequiredAttributeAdapter)
                    },
                    {
                        new CustomRegularExpressionAttribute("abc"),
                        typeof(RegularExpressionAttributeAdapter)
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(DataAnnotationAdapters))]
    public void AdapterFactory_RegistersAdapters_ForDataAnnotationAttributes(
           ValidationAttribute attribute,
           Type expectedAdapterType)
    {
        // Arrange and Act
        var adapter = _validationAttributeAdapterProvider.GetAttributeAdapter(attribute, stringLocalizer: null);

        // Assert
        Assert.IsType(expectedAdapterType, adapter);
    }

    public static TheoryData<ValidationAttribute, string> DataTypeAdapters
    {
        get
        {
            return new TheoryData<ValidationAttribute, string> {
                    { new UrlAttribute(), "data-val-url" },
                    { new CreditCardAttribute(), "data-val-creditcard" },
                    { new EmailAddressAttribute(), "data-val-email" },
                    { new PhoneAttribute(), "data-val-phone" }
                };
        }
    }

    [Theory]
    [MemberData(nameof(DataTypeAdapters))]
    public void AdapterFactory_RegistersAdapters_ForDataTypeAttributes(
        ValidationAttribute attribute,
        string expectedRuleName)
    {
        // Arrange & Act
        var adapter = _validationAttributeAdapterProvider.GetAttributeAdapter(attribute, stringLocalizer: null);

        // Assert
        var dataTypeAdapter = Assert.IsType<DataTypeAttributeAdapter>(adapter);
        Assert.Equal(expectedRuleName, dataTypeAdapter.RuleName);
    }

    class CustomRegularExpressionAttribute : RegularExpressionAttribute
    {
        public CustomRegularExpressionAttribute(string pattern) : base(pattern)
        {
            ErrorMessage = "Not valid.";
        }
    }
}
