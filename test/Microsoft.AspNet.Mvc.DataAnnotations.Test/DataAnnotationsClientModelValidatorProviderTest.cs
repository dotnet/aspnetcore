// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc.DataAnnotations;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class DataAnnotationsClientModelValidatorProviderTest
    {
        private readonly IModelMetadataProvider _metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        [Fact]
        public void GetValidators_AddsRequiredAttribute_ForIsRequiredTrue()
        {
            // Arrange
            var provider = new DataAnnotationsClientModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                stringLocalizerFactory: null);

            var metadata = _metadataProvider.GetMetadataForProperty(
                typeof(DummyRequiredAttributeHelperClass),
                nameof(DummyRequiredAttributeHelperClass.ValueTypeWithoutAttribute));

            var providerContext = new ClientValidatorProviderContext(metadata);

            // Act
            provider.GetValidators(providerContext);

            // Assert
            var validator = Assert.Single(providerContext.Validators);
            Assert.IsType<RequiredAttributeAdapter>(validator);
        }

        [Fact]
        public void GetValidators_DoesNotAddRequiredAttribute_ForIsRequiredFalse()
        {
            // Arrange
            var provider = new DataAnnotationsClientModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                stringLocalizerFactory: null);

            var metadata = _metadataProvider.GetMetadataForProperty(
                typeof(DummyRequiredAttributeHelperClass),
                nameof(DummyRequiredAttributeHelperClass.ReferenceTypeWithoutAttribute));

            var providerContext = new ClientValidatorProviderContext(metadata);

            // Act
            provider.GetValidators(providerContext);

            // Assert
            Assert.Empty(providerContext.Validators);
        }

        [Fact]
        public void GetValidators_DoesNotAddExtraRequiredAttribute_IfAttributeIsSpecifiedExplicitly()
        {
            // Arrange
            var provider = new DataAnnotationsClientModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                stringLocalizerFactory: null);

            var metadata = _metadataProvider.GetMetadataForProperty(
                typeof(DummyRequiredAttributeHelperClass),
                nameof(DummyRequiredAttributeHelperClass.WithAttribute));

            var providerContext = new ClientValidatorProviderContext(metadata);

            // Act
            provider.GetValidators(providerContext);

            // Assert
            var validator = Assert.Single(providerContext.Validators);
            var adapter = Assert.IsType<RequiredAttributeAdapter>(validator);
            Assert.Equal("Custom Required Message", adapter.Attribute.ErrorMessage);
        }

        [Fact]
        public void UnknownValidationAttribute_IsNotAddedAsValidator()
        {
            // Arrange
            var provider = new DataAnnotationsClientModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                stringLocalizerFactory: null);
            var metadata = _metadataProvider.GetMetadataForType(typeof(DummyClassWithDummyValidationAttribute));

            var providerContext = new ClientValidatorProviderContext(metadata);

            // Act
            provider.GetValidators(providerContext);

            // Assert
            Assert.Empty(providerContext.Validators);
        }

        private class DummyValidationAttribute : ValidationAttribute
        {
        }

        [DummyValidation]
        private class DummyClassWithDummyValidationAttribute
        {
        }

        private class DummyRequiredAttributeHelperClass
        {
            [Required(ErrorMessage = "Custom Required Message")]
            public int WithAttribute { get; set; }

            public int ValueTypeWithoutAttribute { get; set; }

            public string ReferenceTypeWithoutAttribute { get; set; }
        }
    }
}
