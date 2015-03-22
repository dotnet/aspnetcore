// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    public class DataMemberRequiredBindingMetadataProviderTest
    {
        [Fact]
        public void IsRequired_SetToTrue_WithDataMemberIsRequiredTrue()
        {
            // Arrange
            var provider = new DataMemberRequiredBindingMetadataProvider();

            var attributes = new object[]
            {
                new DataMemberAttribute() { IsRequired = true, }
            };

            var key = ModelMetadataIdentity.ForProperty(
                typeof(string),
                nameof(ClassWithDataMemberIsRequiredTrue.StringProperty),
                typeof(ClassWithDataMemberIsRequiredTrue));
            var context = new BindingMetadataProviderContext(key, attributes);

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsRequired_LeftAlone_DataMemberIsRequiredFalse(bool? initialValue)
        {
            // Arrange
            var provider = new DataMemberRequiredBindingMetadataProvider();

            var attributes = new object[]
            {
                new DataMemberAttribute() { IsRequired = false, }
            };

            var key = ModelMetadataIdentity.ForProperty(
                typeof(string),
                nameof(ClassWithDataMemberIsRequiredFalse.StringProperty),
                typeof(ClassWithDataMemberIsRequiredFalse));
            var context = new BindingMetadataProviderContext(key, attributes);

            context.BindingMetadata.IsRequired = initialValue;

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsRequired_LeftAlone_ForNonPropertyMetadata(bool? initialValue)
        {
            // Arrange
            var provider = new DataMemberRequiredBindingMetadataProvider();

            var attributes = new object[]
            {
                new DataMemberAttribute() { IsRequired = true, }
            };

            var key = ModelMetadataIdentity.ForType(typeof(ClassWithDataMemberIsRequiredTrue));
            var context = new BindingMetadataProviderContext(key, attributes);

            context.BindingMetadata.IsRequired = initialValue;

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsRequired_LeftAlone_WithoutDataMemberAttribute(bool? initialValue)
        {
            // Arrange
            var provider = new DataMemberRequiredBindingMetadataProvider();

            var key = ModelMetadataIdentity.ForProperty(
                typeof(string),
                nameof(ClassWithoutAttributes.StringProperty),
                typeof(ClassWithoutAttributes));
            var context = new BindingMetadataProviderContext(key, attributes: new object[0]);

            context.BindingMetadata.IsRequired = initialValue;

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsRequired_LeftAlone_WithoutDataContractAttribute(bool? initialValue)
        {
            // Arrange
            var provider = new DataMemberRequiredBindingMetadataProvider();

            var attributes = new object[]
            {
                new DataMemberAttribute() { IsRequired = true, }
            };

            var key = ModelMetadataIdentity.ForProperty(
                typeof(string),
                nameof(ClassWithDataMemberIsRequiredTrueWithoutDataContract.StringProperty),
                typeof(ClassWithDataMemberIsRequiredTrueWithoutDataContract));
            var context = new BindingMetadataProviderContext(key, attributes);

            context.BindingMetadata.IsRequired = initialValue;

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsRequired);
        }

        [DataContract]
        private class ClassWithDataMemberIsRequiredTrue
        {
            [DataMember(IsRequired = true)]
            public string StringProperty { get; set; }
        }

        [DataContract]
        private class ClassWithDataMemberIsRequiredFalse
        {
            [DataMember(IsRequired = false)]
            public string StringProperty { get; set; }
        }

        private class ClassWithDataMemberIsRequiredTrueWithoutDataContract
        {
            [DataMember(IsRequired = true)]
            public string StringProperty { get; set; }
        }

        private class ClassWithoutAttributes
        {
            public string StringProperty { get; set; }
        }
    }
}
