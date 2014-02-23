using System;
using System.Collections.Generic;
using System.Globalization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CompositeModelBinderTest
    {
        [Fact]
        public void BindModel_SuccessfulBind_RunsValidationAndReturnsModel()
        {
            // Arrange
            bool validationCalled = false;

            ModelBindingContext bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(int)),
                ModelName = "someName",
                //ModelState = executionContext.Controller.ViewData.ModelState,
                //PropertyFilter = _ => true,
                ValueProvider = new SimpleValueProvider
                {
                    { "someName", "dummyValue" }
                }
            };

            Mock<IModelBinder> mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                .Returns(
                    delegate(ModelBindingContext context)
                    {
                        Assert.Same(bindingContext.ModelMetadata, context.ModelMetadata);
                        Assert.Equal("someName", context.ModelName);
                        Assert.Same(bindingContext.ValueProvider, context.ValueProvider);

                        context.Model = 42;
                        // TODO: Validation
                        // mbc.ValidationNode.Validating += delegate { validationCalled = true; };
                        return true;
                    });

            //binderProviders.RegisterBinderForType(typeof(int), mockIntBinder.Object, false /* suppressPrefixCheck */);
            IModelBinder shimBinder = new CompositeModelBinder(mockIntBinder.Object);

            // Act
            bool isBound = shimBinder.BindModel(bindingContext);

            // Assert
            Assert.True(isBound);
            Assert.Equal(42, bindingContext.Model);
            // TODO: Validation
            // Assert.True(validationCalled);
            Assert.True(bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void BindModel_SuccessfulBind_ComplexTypeFallback_RunsValidationAndReturnsModel()
        {
            // Arrange
            bool validationCalled = false;
            List<int> expectedModel = new List<int> { 1, 2, 3, 4, 5 };

            ModelBindingContext bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(List<int>)),
                ModelName = "someName",
                //ModelState = executionContext.Controller.ViewData.ModelState,
                //PropertyFilter = _ => true,
                ValueProvider = new SimpleValueProvider
                {
                    { "someOtherName", "dummyValue" }
                }
            };

            Mock<IModelBinder> mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                .Returns(
                    delegate(ModelBindingContext mbc)
                    {
                        if (!String.IsNullOrEmpty(mbc.ModelName))
                        {
                            return false;
                        }

                        Assert.Same(bindingContext.ModelMetadata, mbc.ModelMetadata);
                        Assert.Equal("", mbc.ModelName);
                        Assert.Same(bindingContext.ValueProvider, mbc.ValueProvider);

                        mbc.Model = expectedModel;
                        // TODO: Validation
                        // mbc.ValidationNode.Validating += delegate { validationCalled = true; };
                        return true;
                    });

            //binderProviders.RegisterBinderForType(typeof(List<int>), mockIntBinder.Object, false /* suppressPrefixCheck */);
            IModelBinder shimBinder = new CompositeModelBinder(mockIntBinder.Object);

            // Act
            bool isBound = shimBinder.BindModel(bindingContext);

            // Assert
            Assert.True(isBound);
            Assert.Equal(expectedModel, bindingContext.Model);
            // TODO: Validation
            // Assert.True(validationCalled);
            // Assert.True(bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void BindModel_UnsuccessfulBind_BinderFails_ReturnsNull()
        {
            // Arrange
            Mock<IModelBinder> mockListBinder = new Mock<IModelBinder>();
            mockListBinder.Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                          .Returns(false)
                          .Verifiable();

            IModelBinder shimBinder = (IModelBinder)mockListBinder.Object;

            ModelBindingContext bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = false,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(List<int>)),
            };

            // Act
            bool isBound = shimBinder.BindModel(bindingContext);

            // Assert
            Assert.False(isBound);
            Assert.Null(bindingContext.Model);
            // TODO: Validation
            // Assert.True(bindingContext.ModelState.IsValid);
            mockListBinder.Verify();
        }

        [Fact]
        public void BindModel_UnsuccessfulBind_SimpleTypeNoFallback_ReturnsNull()
        {
            // Arrange
            var innerBinder = Mock.Of<IModelBinder>();
            CompositeModelBinder shimBinder = new CompositeModelBinder(innerBinder);

            ModelBindingContext bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(int)),
                //ModelState = executionContext.Controller.ViewData.ModelState
            };

            // Act
            bool isBound = shimBinder.BindModel(bindingContext);

            // Assert
            Assert.False(isBound);
            Assert.Null(bindingContext.Model);
        }

        private class SimpleModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private class SimpleValueProvider : Dictionary<string, object>, IValueProvider
        {
            private readonly CultureInfo _culture;

            public SimpleValueProvider()
                : this(null)
            {
            }

            public SimpleValueProvider(CultureInfo culture)
                : base(StringComparer.OrdinalIgnoreCase)
            {
                _culture = culture ?? CultureInfo.InvariantCulture;
            }

            // copied from ValueProviderUtil
            public bool ContainsPrefix(string prefix)
            {
                foreach (string key in Keys)
                {
                    if (key != null)
                    {
                        if (prefix.Length == 0)
                        {
                            return true; // shortcut - non-null key matches empty prefix
                        }

                        if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            if (key.Length == prefix.Length)
                            {
                                return true; // exact match
                            }
                            else
                            {
                                switch (key[prefix.Length])
                                {
                                    case '.': // known separator characters
                                    case '[':
                                        return true;
                                }
                            }
                        }
                    }
                }

                return false; // nothing found
            }

            public ValueProviderResult GetValue(string key)
            {
                object rawValue;
                if (TryGetValue(key, out rawValue))
                {
                    return new ValueProviderResult(rawValue, Convert.ToString(rawValue, _culture), _culture);
                }
                else
                {
                    // value not found
                    return null;
                }
            }
        }
    }
}
