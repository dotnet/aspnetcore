using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class TypeConverterModelBinderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Calendar))]
        [InlineData(typeof(TestClass))]
        public async Task BindModel_ReturnsFalse_IfTypeCannotBeConverted(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "some-value" }
            };

            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(retVal);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(double))]
        [InlineData(typeof(DayOfWeek))]
        public async Task BindModel_ReturnsTrue_IfTypeCanBeConverted(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "some-value" }
            };

            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
        }

        [Fact]
        public async Task BindModel_Error_FormatExceptionsTurnedIntoStringsInModelState()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "not an integer" }
            };

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Null(bindingContext.Model);
            Assert.Equal(false, bindingContext.ModelState.IsValid);
            Assert.Equal("Input string was not in a correct format.", bindingContext.ModelState["theModelName"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task BindModel_NullValueProviderResult_ReturnsFalse()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(int));

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(retVal, "BindModel should have returned null.");
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResult_ConvertEmptyStringsToNull()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(string));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "" }
            };

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Null(bindingContext.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResult_ReturnsModel()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "42" }
            };

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Equal(42, bindingContext.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            return new ModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, modelType),
                ModelName = "theModelName",
                ValueProvider = new SimpleHttpValueProvider() // empty
            };
        }

        private sealed class TestClass
        {
        }
    }
}
