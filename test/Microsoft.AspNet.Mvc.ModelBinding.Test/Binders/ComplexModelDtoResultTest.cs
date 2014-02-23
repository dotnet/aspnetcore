using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ComplexModelDtoResultTest
    {
        [Fact]
        public void Constructor_ThrowsIfValidationNodeIsNull()
        {
            // Act & assert
            ExceptionAssert.ThrowsArgumentNull(
                () => new ComplexModelDtoResult("some string"),
                "validationNode");
        }

        // TODO: Validation
        //[Fact]
        //public void Constructor_SetsProperties()
        //{
        //    // Arrange
        //    ModelValidationNode validationNode = GetValidationNode();

        //    // Act
        //    ComplexModelDtoResult result = new ComplexModelDtoResult("some string", validationNode);

        //    // Assert
        //    Assert.Equal("some string", result.Model);
        //    Assert.Equal(validationNode, result.ValidationNode);
        //}

        //private static ModelValidationNode GetValidationNode()
        //{
        //    EmptyModelMetadataProvider provider = new EmptyModelMetadataProvider();
        //    ModelMetadata metadata = provider.GetMetadataForType(null, typeof(object));
        //    return new ModelValidationNode(metadata, "someKey");
        //}
    }
}
