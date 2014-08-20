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
            Assert.Equal(typeof(RazorViewEngine), mvcOptions.ViewEngines[0].OptionType);
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
            Assert.Equal(6, mvcOptions.ModelBinders.Count);
            Assert.Equal(typeof(TypeConverterModelBinder), mvcOptions.ModelBinders[0].OptionType);
            Assert.Equal(typeof(TypeMatchModelBinder), mvcOptions.ModelBinders[1].OptionType);
            Assert.Equal(typeof(ByteArrayModelBinder), mvcOptions.ModelBinders[2].OptionType);
            Assert.Equal(typeof(GenericModelBinder), mvcOptions.ModelBinders[3].OptionType);
            Assert.Equal(typeof(MutableObjectModelBinder), mvcOptions.ModelBinders[4].OptionType);
            Assert.Equal(typeof(ComplexModelDtoModelBinder), mvcOptions.ModelBinders[5].OptionType);
        }

        [Fact]
        public void Setup_SetsUpValueProviders()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Setup(mvcOptions);

            // Assert
            var valueProviders = mvcOptions.ValueProviderFactories;
            Assert.Equal(3, valueProviders.Count);
            Assert.IsType<RouteValueValueProviderFactory>(valueProviders[0].Instance);
            Assert.IsType<QueryStringValueProviderFactory>(valueProviders[1].Instance);
            Assert.IsType<FormValueProviderFactory>(valueProviders[2].Instance);
        }

        [Fact]
        public void Setup_SetsUpOutputFormatters()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Setup(mvcOptions);

            // Assert
            Assert.Equal(5, mvcOptions.OutputFormatters.Count);
            Assert.IsType<NoContentFormatter>(mvcOptions.OutputFormatters[0].Instance);
            Assert.IsType<TextPlainFormatter>(mvcOptions.OutputFormatters[1].Instance);
            Assert.IsType<JsonOutputFormatter>(mvcOptions.OutputFormatters[2].Instance);
            Assert.IsType<XmlDataContractSerializerOutputFormatter>(mvcOptions.OutputFormatters[3].Instance);
            Assert.IsType<XmlSerializerOutputFormatter>(mvcOptions.OutputFormatters[4].Instance);
        }

        [Fact]
        public void Setup_SetsUpInputFormatters()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Setup(mvcOptions);

            // Assert
            Assert.Equal(3, mvcOptions.InputFormatters.Count);
            Assert.IsType<JsonInputFormatter>(mvcOptions.InputFormatters[0].Instance);
            Assert.IsType<XmlSerializerInputFormatter>(mvcOptions.InputFormatters[1].Instance);
            Assert.IsType<XmlDataContractSerializerInputFormatter>(mvcOptions.InputFormatters[2].Instance);
        }

        [Fact]
        public void Setup_SetsUpModelValidatorProviders()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Setup(mvcOptions);

            // Assert
            Assert.Equal(2, mvcOptions.ModelValidatorProviders.Count);
            Assert.IsType<DataAnnotationsModelValidatorProvider>(mvcOptions.ModelValidatorProviders[0].Instance);
            Assert.IsType<DataMemberModelValidatorProvider>(mvcOptions.ModelValidatorProviders[1].Instance);
        }
    }
}