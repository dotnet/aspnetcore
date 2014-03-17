using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ComplexModelDtoResultTest
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            // Arrange
            var validationNode = GetValidationNode();

            // Act
            var result = new ComplexModelDtoResult("some string", validationNode);

            // Assert
            Assert.Equal("some string", result.Model);
            Assert.Equal(validationNode, result.ValidationNode);
        }

        private static ModelValidationNode GetValidationNode()
        {
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(null, typeof(object));
            return new ModelValidationNode(metadata, "someKey");
    }
}
}
