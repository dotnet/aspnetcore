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
            Assert.Equal("The value 'not an integer' is not valid for Int32.", bindingContext.ModelState["theModelName"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void BindModel_Error_FormatExceptionsTurnedIntoStringsInModelState_ErrorNotAddedIfCallbackReturnsNull()
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
            Assert.True(bindingContext.ModelState.IsValid);
        }

        // TODO: TypeConverter
        //[Fact]
        //public void BindModel_Error_GeneralExceptionsSavedInModelState()
        //{
        //    // Arrange
        //    ModelBindingContext bindingContext = GetBindingContext(typeof(Dummy));
        //    bindingContext.ValueProvider = new SimpleHttpValueProvider
        //    {
        //        { "theModelName", "foo" }
        //    };

        //    TypeConverterModelBinder binder = new TypeConverterModelBinder();

        //    // Act
        //    bool retVal = binder.BindModel(bindingContext);

        //    // Assert
        //    Assert.False(retVal);
        //    Assert.Null(bindingContext.Model);
        //    Assert.Equal("The parameter conversion from type 'System.String' to type 'Microsoft.AspNet.Mvc.ModelBinding.Test.TypeConverterModelBinderTest+Dummy' failed. See the inner exception for more information.", bindingContext.ModelState["theModelName"].Errors[0].Exception.Message);
        //    Assert.Equal("From DummyTypeConverter: foo", bindingContext.ModelState["theModelName"].Errors[0].Exception.InnerException.Message);
        //}

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

        // TODO: TypeConverter
        //[TypeConverter(typeof(DummyTypeConverter))]
        //private struct Dummy
        //{
        //}

        private sealed class DummyTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                throw new InvalidOperationException(String.Format("From DummyTypeConverter: {0}", value));
            }
        }
    }
}
