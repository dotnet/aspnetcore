using System;
using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class TypeConverterModelBinderTest
    {
        // private static readonly ModelBinderErrorMessageProvider  = (modelMetadata, incomingValue) => null;

        [Fact]
        public void BindModel_Error_FormatExceptionsTurnedIntoStringsInModelState()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "not an integer" }
            };

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.False(retVal);
            Assert.Null(bindingContext.Model);
            Assert.Equal(false, bindingContext.ModelState.IsValid);
            Assert.Equal("Input string was not in a correct format.", bindingContext.ModelState["theModelName"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void BindModel_NullValueProviderResult_ReturnsFalse()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(int));

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.False(retVal, "BindModel should have returned null.");
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public void BindModel_ValidValueProviderResult_ConvertEmptyStringsToNull()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(string));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "" }
            };

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Null(bindingContext.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public void BindModel_ValidValueProviderResult_ReturnsModel()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "42" }
            };

            TypeConverterModelBinder binder = new TypeConverterModelBinder();

            // Act
            bool retVal = binder.BindModel(bindingContext);

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
    }
}
