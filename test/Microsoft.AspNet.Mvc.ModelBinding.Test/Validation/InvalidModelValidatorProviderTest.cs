using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class InvalidModelValidatorProviderTest
    {
        private static DataAnnotationsModelMetadataProvider _metadataProvider = new DataAnnotationsModelMetadataProvider();

        [Fact]
        public void GetValidatorsReturnsNothingForValidModel()
        {
            // Arrange
            var validatorProvider = new InvalidModelValidatorProvider();

            // Act
            var validators = validatorProvider.GetValidators(_metadataProvider.GetMetadataForType(null, typeof(ValidModel)));

            // Assert
            Assert.Empty(validators);
        }

        [Fact]
        public void GetValidatorsReturnsInvalidModelValidatorsForInvalidModelType()
        {
            // Arrange
            var name = typeof(InvalidModel).FullName;
            var validatorProvider = new InvalidModelValidatorProvider();

            // Act
            var validators = validatorProvider.GetValidators(_metadataProvider.GetMetadataForType(null, typeof(InvalidModel)));

            // Assert
            Assert.Equal(2, validators.Count());
            ExceptionAssert.Throws<InvalidOperationException>(() => validators.ElementAt(0).Validate(null),
                "Non-public property 'Internal' on type '" + name  + "' is attributed with one or more validation attributes. Validation attributes on non-public properties are not supported. Consider using a public property for validation instead.");
            ExceptionAssert.Throws<InvalidOperationException>(() => validators.ElementAt(1).Validate(null),
                "Field 'Field' on type '" + name + "' is attributed with one or more validation attributes. Validation attributes on fields are not supported. Consider using a public property for validation instead.");
        }

        [Fact]
        public void GetValidatorsReturnsInvalidModelValidatorsForInvalidModelProperty()
        {
            // Arrange
            var name = typeof(InvalidModel).FullName;
            var validatorProvider = new InvalidModelValidatorProvider();

            // Act
            var validators = validatorProvider.GetValidators(_metadataProvider.GetMetadataForProperty(null, typeof(InvalidModel), "Value"));

            // Assert
            Assert.Equal(1, validators.Count());
            ExceptionAssert.Throws<InvalidOperationException>(() => validators.First().Validate(null),
                "Property 'Value' on type '" + name + "' is invalid. Value-typed properties marked as [Required] must also be marked with [DataMember(IsRequired=true)] to be recognized as required. Consider attributing the declaring type with [DataContract] and the property with [DataMember(IsRequired=true)].");
        }

        [DataContract]
        public class ValidModel
        {
            [Required]
            [DataMember]
            [StringLength(10)]
            public string Ref { get; set; }

            [DataMember]
            internal string Internal { get; set; }

            [Required]
            [DataMember(IsRequired = true)]
            public int Value { get; set; }

            public string Field;
        }

        public class InvalidModel
        {
            [Required]
            public string Ref { get; set; }

            [StringLength(10)]
            [RegularExpression("pattern")]
            internal string Internal { get; set; }

            [Required]
            public int Value { get; set; }

            [StringLength(10)]
            public string Field;
        }
    }
}
