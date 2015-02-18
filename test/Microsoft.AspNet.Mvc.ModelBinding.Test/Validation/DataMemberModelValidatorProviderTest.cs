// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DataMemberModelValidatorProviderTest
    {
        private readonly DataAnnotationsModelMetadataProvider _metadataProvider = new DataAnnotationsModelMetadataProvider();

        [Fact]
        public void ClassWithoutAttributes_NoValidator()
        {
            // Arrange
            var provider = new DataMemberModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForProperty(typeof(ClassWithoutAttributes), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Empty(validators);
        }

        private class ClassWithoutAttributes
        {
            public int TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredTrue_Validator()
        {
            // Arrange
            var provider = new DataMemberModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForProperty(typeof(ClassWithDataMemberIsRequiredTrue), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            var validator = Assert.Single(validators);
            Assert.True(validator.IsRequired);
        }

        [DataContract]
        private class ClassWithDataMemberIsRequiredTrue
        {
            [DataMember(IsRequired = true)]
            public int TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredFalse_NoValidator()
        {
            // Arrange
            var provider = new DataMemberModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForProperty(typeof(ClassWithDataMemberIsRequiredFalse), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Empty(validators);
        }

        [DataContract]
        private class ClassWithDataMemberIsRequiredFalse
        {
            [DataMember(IsRequired = false)]
            public int TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredTrueWithoutDataContract_NoValidator()
        {
            // Arrange
            var provider = new DataMemberModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForProperty(typeof(ClassWithDataMemberIsRequiredTrueWithoutDataContract), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Empty(validators);
        }

        private class ClassWithDataMemberIsRequiredTrueWithoutDataContract
        {
            [DataMember(IsRequired = true)]
            public int TheProperty { get; set; }
        }
    }
}
