// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class ModelBindingHelperTest
    {
        [Fact]
        public async Task TryUpdateModel_ReturnsFalse_IfBinderReturnsFalse()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider.Setup(m => m.GetMetadataForType(null, It.IsAny<Type>()))
                            .Returns(new ModelMetadata(metadataProvider.Object, null, null, typeof(MyModel), null))
                            .Verifiable();

            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Returns(Task.FromResult(false));
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    null,
                                                    Mock.Of<HttpContext>(),
                                                    new ModelStateDictionary(),
                                                    metadataProvider.Object,
                                                    GetCompositeBinder(binder.Object),
                                                    Mock.Of<IValueProvider>(),
                                                    Enumerable.Empty<IModelValidatorProvider>());

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            metadataProvider.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsFalse_IfModelValidationFails()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new TypeConverterModelBinder(),
                new ComplexModelDtoModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider();
            var model = new MyModel();
            var modelStateDictionary = new ModelStateDictionary();
            var values = new Dictionary<string, object>
            {
                { "", null }
            };
            var valueProvider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    "",
                                                    Mock.Of<HttpContext>(),
                                                    modelStateDictionary,
                                                    new DataAnnotationsModelMetadataProvider(),
                                                    GetCompositeBinder(binders),
                                                    valueProvider,
                                                    new[] { validator });

            // Assert
            Assert.False(result);
            Assert.Equal("The MyProperty field is required.",
                         modelStateDictionary["MyProperty"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsTrue_IfModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new TypeConverterModelBinder(),
                new ComplexModelDtoModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider();
            var model = new MyModel { MyProperty = "Old-Value" };
            var modelStateDictionary = new ModelStateDictionary();
            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" }
            };
            var valueProvider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    "",
                                                    Mock.Of<HttpContext>(),
                                                    modelStateDictionary,
                                                    new DataAnnotationsModelMetadataProvider(),
                                                    GetCompositeBinder(binders),
                                                    valueProvider,
                                                    new[] { validator });

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
        }

        private static IModelBinder GetCompositeBinder(params IModelBinder[] binders)
        {
            var binderProvider = new Mock<IModelBinderProvider>();
            binderProvider.SetupGet(p => p.ModelBinders)
                          .Returns(binders);
            return new CompositeModelBinder(binderProvider.Object);
        }

        private class MyModel
        {
            [Required]
            public string MyProperty { get; set; }
        }
    }
}
#endif