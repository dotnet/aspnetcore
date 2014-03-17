using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ErrorModelValidatorTest
    {
        private readonly DataAnnotationsModelMetadataProvider _metadataProvider = new DataAnnotationsModelMetadataProvider();

        [Fact]
        public void ValidateThrowsException()
        {
            // Arrange
            var validator = new ErrorModelValidator("error");

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => validator.Validate(null), "error");
        }
    }
}
