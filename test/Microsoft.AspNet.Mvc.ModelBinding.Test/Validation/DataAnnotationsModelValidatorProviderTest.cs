// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
#if NET45
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DataAnnotationsModelValidatorProviderTest
    {
        private readonly DataAnnotationsModelMetadataProvider _metadataProvider = new DataAnnotationsModelMetadataProvider();

        public static IEnumerable<object[]> DataAnnotationAdapters
        {
            get
            {
                yield return new object[] { new RegularExpressionAttribute("abc"),
                                            typeof(RegularExpressionAttributeAdapter) };

                yield return new object[] { new MaxLengthAttribute(),
                                            typeof(MaxLengthAttributeAdapter) };

                yield return new object[] { new MinLengthAttribute(1),
                                            typeof(MinLengthAttributeAdapter) };

                yield return new object[] { new RangeAttribute(1, 100),
                                            typeof(RangeAttributeAdapter) };

                yield return new object[] { new StringLengthAttribute(6),
                                            typeof(StringLengthAttributeAdapter) };

                yield return new object[] { new RequiredAttribute(),
                                            typeof(RequiredAttributeAdapter) };
            }
        }

        [Theory]
        [MemberData("DataAnnotationAdapters")]
        public void AdapterFactory_RegistersAdapters_ForDataAnnotationAttributes(ValidationAttribute attribute,
                                                                                 Type expectedAdapterType)
        {
            // Arrange
            var adapters = new DataAnnotationsModelValidatorProvider().AttributeFactories;
            var adapterFactory = adapters.Single(kvp => kvp.Key == attribute.GetType()).Value;

            // Act
            var adapter = adapterFactory(attribute);

            // Assert
            Assert.IsType(expectedAdapterType, adapter);
        }

        public static IEnumerable<object[]> DataTypeAdapters
        {
            get
            {
                yield return new object[] { new UrlAttribute(), "url" };
                yield return new object[] { new CreditCardAttribute(), "creditcard" };
                yield return new object[] { new EmailAddressAttribute(), "email" };
                yield return new object[] { new PhoneAttribute(), "phone" };
            }
        }

        [Theory]
        [MemberData("DataTypeAdapters")]
        public void AdapterFactory_RegistersAdapters_ForDataTypeAttributes(ValidationAttribute attribute,
                                                                           string expectedRuleName)
        {
            // Arrange
            var adapters = new DataAnnotationsModelValidatorProvider().AttributeFactories;
            var adapterFactory = adapters.Single(kvp => kvp.Key == attribute.GetType()).Value;

            // Act
            var adapter = adapterFactory(attribute);

            // Assert
            var dataTypeAdapter = Assert.IsType<DataTypeAttributeAdapter>(adapter);
            Assert.Equal(expectedRuleName, dataTypeAdapter.RuleName);
        }

        [Fact]
        public void UnknownValidationAttributeGetsDefaultAdapter()
        {
            // Arrange
            var provider = new DataAnnotationsModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForType(() => null, typeof(DummyClassWithDummyValidationAttribute));

            // Act
            IEnumerable<IModelValidator> validators = provider.GetValidators(metadata);

            // Assert
            var validator = validators.Single();
            Assert.IsType<DataAnnotationsModelValidator>(validator);
        }

        private class DummyValidationAttribute : ValidationAttribute
        {
        }

        [DummyValidation]
        private class DummyClassWithDummyValidationAttribute
        {
        }

        // Default IValidatableObject adapter factory

#if NET45
        [Fact]
        public void IValidatableObjectGetsAValidator()
        {
            // Arrange
            var provider = new DataAnnotationsModelValidatorProvider();
            var mockValidatable = new Mock<IValidatableObject>();
            var metadata = _metadataProvider.GetMetadataForType(() => null, mockValidatable.Object.GetType());

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Single(validators);
        }
#endif

        // Integration with metadata system

        [Fact]
        public void DoesNotReadPropertyValue()
        {
            // Arrange
            var provider = new DataAnnotationsModelValidatorProvider();
            var model = new ObservableModel();
            var metadata = _metadataProvider.GetMetadataForProperty(() => model.TheProperty, typeof(ObservableModel), "TheProperty");
            var context = new ModelValidationContext(null, null, null, metadata, null);

            // Act
            var validators = provider.GetValidators(metadata).ToArray();
            var results = validators.SelectMany(o => o.Validate(context)).ToArray();

            // Assert
            Assert.Empty(validators);
            Assert.False(model.PropertyWasRead());
        }

        private class ObservableModel
        {
            private bool _propertyWasRead;

            public string TheProperty
            {
                get
                {
                    _propertyWasRead = true;
                    return "Hello";
                }
            }

            public bool PropertyWasRead()
            {
                return _propertyWasRead;
            }
        }

        private class BaseModel
        {
            public virtual string MyProperty { get; set; }
        }

        private class DerivedModel : BaseModel
        {
            [StringLength(10)]
            public override string MyProperty
            {
                get { return base.MyProperty; }
                set { base.MyProperty = value; }
            }
        }
    }
}
