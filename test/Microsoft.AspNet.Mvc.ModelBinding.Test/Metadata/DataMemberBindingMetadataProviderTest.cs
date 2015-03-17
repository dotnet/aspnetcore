// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    public class DataMemberBindingMetadataProviderTest
    {
        [Fact]
        public void ClassWithoutAttributes_NotRequired()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ClassWithoutAttributes),
                "TheProperty");

            // Assert
            Assert.False(metadata.IsRequired);
        }

        private class ClassWithoutAttributes
        {
            public string TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredTrue_Validator()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ClassWithDataMemberIsRequiredTrue),
                "TheProperty");

            // Assert
            Assert.True(metadata.IsRequired);
        }

        [DataContract]
        private class ClassWithDataMemberIsRequiredTrue
        {
            [DataMember(IsRequired = true)]
            public string TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredFalse_NoValidator()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ClassWithDataMemberIsRequiredFalse),
                "TheProperty");

            // Assert
            Assert.False(metadata.IsRequired);
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
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ClassWithDataMemberIsRequiredTrueWithoutDataContract),
                "TheProperty");

            // Assert
            Assert.False(metadata.IsRequired);
        }

        private class ClassWithDataMemberIsRequiredTrueWithoutDataContract
        {
            [DataMember(IsRequired = true)]
            public int TheProperty { get; set; }
        }
    }
}
