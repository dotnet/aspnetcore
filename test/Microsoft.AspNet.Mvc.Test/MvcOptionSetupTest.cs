// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class MvcOptionSetupTest
    {
        [Fact]
        public void Setup_SetsUpViewEngines()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Setup(mvcOptions);

            // Assert
            Assert.Equal(1, mvcOptions.ViewEngines.Count);
            Assert.Equal(typeof(RazorViewEngine), mvcOptions.ViewEngines[0].ViewEngineType);
        }

        [Fact]
        public void Setup_SetsUpModelBinders()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Setup(mvcOptions);

            // Assert
            Assert.Equal(5, mvcOptions.ModelBinders.Count);
            Assert.Equal(typeof(TypeConverterModelBinder), mvcOptions.ModelBinders[0].ModelBinderType);
            Assert.Equal(typeof(TypeMatchModelBinder), mvcOptions.ModelBinders[1].ModelBinderType);
            Assert.Equal(typeof(GenericModelBinder), mvcOptions.ModelBinders[2].ModelBinderType);
            Assert.Equal(typeof(MutableObjectModelBinder), mvcOptions.ModelBinders[3].ModelBinderType);
            Assert.Equal(typeof(ComplexModelDtoModelBinder), mvcOptions.ModelBinders[4].ModelBinderType);
        }
    }
}