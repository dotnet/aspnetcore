using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ComplexModelDtoTest
    {
        [Fact]
        public void ConstructorThrowsIfModelMetadataIsNull()
        {
            // Act & assert
            ExceptionAssert.ThrowsArgumentNull(
                () => new ComplexModelDto(null, Enumerable.Empty<ModelMetadata>()),
                "modelMetadata");
        }

        [Fact]
        public void ConstructorThrowsIfPropertyMetadataIsNull()
        {
            // Arrange
            ModelMetadata modelMetadata = GetModelMetadata();

            // Act & assert
            ExceptionAssert.ThrowsArgumentNull(
                () => new ComplexModelDto(modelMetadata, null),
                "propertyMetadata");
        }

        [Fact]
        public void ConstructorSetsProperties()
        {
            // Arrange
            ModelMetadata modelMetadata = GetModelMetadata();
            ModelMetadata[] propertyMetadata = new ModelMetadata[0];

            // Act
            ComplexModelDto dto = new ComplexModelDto(modelMetadata, propertyMetadata);

            // Assert
            Assert.Equal(modelMetadata, dto.ModelMetadata);
            Assert.Equal(propertyMetadata, dto.PropertyMetadata.ToArray());
            Assert.Empty(dto.Results);
        }

        private static ModelMetadata GetModelMetadata()
        {
            return new ModelMetadata(new EmptyModelMetadataProvider(), typeof(object), null, typeof(object), "PropertyName");
        }
    }
}
