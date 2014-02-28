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
            var metadata = _metadataProvider.GetMetadataForProperty(() => null, typeof(ClassWithoutAttributes), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Empty(validators);
        }

        class ClassWithoutAttributes
        {
            public int TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredTrue_Validator()
        {
            // Arrange
            var provider = new DataMemberModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForProperty(() => null, typeof(ClassWithDataMemberIsRequiredTrue), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            var validator = Assert.Single(validators);
            Assert.True(validator.IsRequired);
        }

        [DataContract]
        class ClassWithDataMemberIsRequiredTrue
        {
            [DataMember(IsRequired = true)]
            public int TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredFalse_NoValidator()
        {
            // Arrange
            var provider = new DataMemberModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForProperty(() => null, typeof(ClassWithDataMemberIsRequiredFalse), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Empty(validators);
        }

        [DataContract]
        class ClassWithDataMemberIsRequiredFalse
        {
            [DataMember(IsRequired = false)]
            public int TheProperty { get; set; }
        }

        [Fact]
        public void ClassWithDataMemberIsRequiredTrueWithoutDataContract_NoValidator()
        {
            // Arrange
            var provider = new DataMemberModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForProperty(() => null, typeof(ClassWithDataMemberIsRequiredTrueWithoutDataContract), "TheProperty");

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Empty(validators);
        }

        class ClassWithDataMemberIsRequiredTrueWithoutDataContract
        {
            [DataMember(IsRequired = true)]
            public int TheProperty { get; set; }
        }
    }
}
