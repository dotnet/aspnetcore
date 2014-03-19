#if NET45
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class KeyValuePairModelBinderTest
    {
        [Fact]
        public void BindModel_MissingKey_ReturnsFalse()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();
            var bindingContext = GetBindingContext(valueProvider, Mock.Of<IModelBinder>());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            bool retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.False(retVal);
            Assert.Null(bindingContext.Model);
            Assert.Empty(bindingContext.ValidationNode.ChildNodes);
        }

        [Fact]
        public void BindModel_MissingValue_ReturnsTrue()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();
            var bindingContext = GetBindingContext(valueProvider);
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            bool retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Null(bindingContext.Model);
            Assert.Equal(new[] { "someName.key" }, bindingContext.ValidationNode.ChildNodes.Select(n => n.ModelStateKey).ToArray());
        }

        [Fact]
        public void BindModel_SubBindingSucceeds()
        {
            // Arrange
            var innerBinder = new CompositeModelBinder(CreateStringBinder(), CreateIntBinder());
            var valueProvider = new SimpleHttpValueProvider();
            var bindingContext = GetBindingContext(valueProvider, innerBinder);
            
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Equal(new KeyValuePair<int, string>(42, "some-value"), bindingContext.Model);
            Assert.Equal(new[] { "someName.key", "someName.value" }, bindingContext.ValidationNode.ChildNodes.Select(n => n.ModelStateKey).ToArray());
        }

        [Fact]
        public void TryBindStrongModel_BinderExists_BinderReturnsCorrectlyTypedObject_ReturnsTrue()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(new SimpleHttpValueProvider());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            int model;
            var retVal = binder.TryBindStrongModel(bindingContext, "key", out model);

            // Assert
            Assert.True(retVal);
            Assert.Equal(42, model);
            Assert.Single(bindingContext.ValidationNode.ChildNodes);
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public void TryBindStrongModel_BinderExists_BinderReturnsIncorrectlyTypedObject_ReturnsTrue()
        {
            // Arrange
            var innerBinder = new Mock<IModelBinder>();
            innerBinder
                .Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    Assert.Equal("someName.key", mbc.ModelName);
                    return true;
                });
            var bindingContext = GetBindingContext(new SimpleHttpValueProvider(), innerBinder.Object);
            

            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            int model;
            var retVal = binder.TryBindStrongModel(bindingContext, "key", out model);

            // Assert
            Assert.True(retVal);
            Assert.Equal(default(int), model);
            Assert.Single(bindingContext.ValidationNode.ChildNodes);
            Assert.Empty(bindingContext.ModelState);
        }

        private static ModelBindingContext GetBindingContext(IValueProvider valueProvider, IModelBinder innerBinder = null)
        {
            var metataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metataProvider.GetMetadataForType(null, typeof(KeyValuePair<int, string>)),
                ModelName = "someName",
                ValueProvider = valueProvider,
                ModelBinder = innerBinder ?? CreateIntBinder(),
                MetadataProvider = metataProvider,
                ValidatorProviders = Enumerable.Empty<IModelValidatorProvider>()
            };
            return bindingContext;
        }
        
        private static IModelBinder CreateIntBinder()
        {
            var mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    if (mbc.ModelType == typeof(int))
                    {
                        mbc.Model = 42;
                        return true;
                    }
                    return false;
                });
            return mockIntBinder.Object;
        }

        private static IModelBinder CreateStringBinder()
        {
            var mockStringBinder = new Mock<IModelBinder>();
            mockStringBinder
                .Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    if (mbc.ModelType == typeof(string))
                    {
                        mbc.Model = "some-value";
                        return true;
                    }
                    return false;
                });
            return mockStringBinder.Object;
        }
    }
}
#endif
