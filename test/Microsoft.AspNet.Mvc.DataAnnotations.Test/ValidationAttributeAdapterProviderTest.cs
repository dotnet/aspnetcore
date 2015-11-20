// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc.DataAnnotations;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
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
                    { new UrlAttribute(), "url" },
                    { new CreditCardAttribute(), "creditcard" },
                    { new EmailAddressAttribute(), "email" },
                    { new PhoneAttribute(), "phone" }
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
    }
}
