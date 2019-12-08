using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Microsoft.AspNetCore.Mvc.ModelBinding.Binders.TrimModelBinderProviderTest;
using static Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadataBindingDetailsProviderTest;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class TrimModelBinderTest
    {
        [Theory]
        [InlineData(true, TrimType.Trim, " value ", "value")]
        [InlineData(true, TrimType.TrimEnd, " value ", " value")]
        [InlineData(true, TrimType.TrimStart, "\t\r\n value ", "value ")]
        [InlineData(false, TrimType.Trim, " value  ", " value  ")]
        public async Task CanTrim(bool canTrim, TrimType trimType, string actualValue, string expectedValue)
        {
            //arrange
            var bindingContext = GetBindingContext(canTrim, trimType);
            var binder = new TrimModelBinder(NullLoggerFactory.Instance);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", actualValue }
            };

            //act
            await binder.BindModelAsync(bindingContext);

            //assert
            Assert.Equal(expectedValue, bindingContext.Result.Model);

        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t \r\n ")]
        public async Task CanTrim_WhenNotConvertEmptyStringToNull(string value)
        {
            // Arrange
            var bindingContext = GetBindingContext(true, TrimType.Trim, false);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", value }
            };

            var binder = new TrimModelBinder(NullLoggerFactory.Instance); 

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(string.Empty, bindingContext.Result.Model);
        }

        private static DefaultModelBindingContext GetBindingContext(bool canTrim, TrimType trimType, bool convertEmptyStringToNull = true)
        {
            return new DefaultModelBindingContext
            {
                ModelMetadata = ModelMetadata(canTrim, trimType, convertEmptyStringToNull),
                ModelName = "theModelName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleValueProvider() // empty
            };
        }
    }
}
