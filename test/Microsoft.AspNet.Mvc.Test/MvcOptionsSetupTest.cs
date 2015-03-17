// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Razor;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class MvcOptionsSetupTest
    {
        [Fact]
        public void Setup_SetsUpViewEngines()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Configure(mvcOptions);

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
            setup.Configure(mvcOptions);

            // Assert
            var i = 0;
            Assert.Equal(13, mvcOptions.ModelBinders.Count);
            Assert.Equal(typeof(BinderTypeBasedModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(ServicesModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(BodyModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(HeaderModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(TypeConverterModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(TypeMatchModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(CancellationTokenModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(ByteArrayModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(FormFileModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(FormCollectionModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(GenericModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(MutableObjectModelBinder), mvcOptions.ModelBinders[i++].OptionType);
            Assert.Equal(typeof(ComplexModelDtoModelBinder), mvcOptions.ModelBinders[i++].OptionType);
        }

        [Fact]
        public void Setup_SetsUpValueProviders()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Configure(mvcOptions);

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
            setup.Configure(mvcOptions);

            // Assert
            Assert.Equal(4, mvcOptions.OutputFormatters.Count);
            Assert.IsType<HttpNoContentOutputFormatter>(mvcOptions.OutputFormatters[0].Instance);
            Assert.IsType<StringOutputFormatter>(mvcOptions.OutputFormatters[1].Instance);
            Assert.IsType<StreamOutputFormatter>(mvcOptions.OutputFormatters[2].Instance);
            Assert.IsType<JsonOutputFormatter>(mvcOptions.OutputFormatters[3].Instance);
        }

        [Fact]
        public void Setup_SetsUpInputFormatters()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Configure(mvcOptions);

            // Assert
            Assert.Equal(1, mvcOptions.InputFormatters.Count);
            Assert.IsType<JsonInputFormatter>(mvcOptions.InputFormatters[0].Instance);
        }

        [Fact]
        public void Setup_SetsUpModelValidatorProviders()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Configure(mvcOptions);

            // Assert
            Assert.Equal(2, mvcOptions.ModelValidatorProviders.Count);
            Assert.IsType<DefaultModelValidatorProvider>(mvcOptions.ModelValidatorProviders[0].Instance);
            Assert.IsType<DataAnnotationsModelValidatorProvider>(mvcOptions.ModelValidatorProviders[1].Instance);
        }

        [Fact]
        public void Setup_IgnoresAcceptHeaderHavingWildCardMediaAndSubMediaTypes()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Configure(mvcOptions);

            // Assert
            Assert.False(mvcOptions.RespectBrowserAcceptHeader);
        }

        [Fact]
        public void Setup_SetsUpExcludeFromValidationDelegates()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            // Act
            setup.Configure(mvcOptions);

            // Assert
            Assert.Equal(8, mvcOptions.ValidationExcludeFilters.Count);
            var i = 0;

            // Verify if the delegates registered by default exclude the given types.
            Assert.Equal(typeof(SimpleTypesExcludeFilter), mvcOptions.ValidationExcludeFilters[i++].OptionType);
            Assert.Equal(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i].OptionType);
            var xObjectFilter 
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++].Instance);
            Assert.Equal(xObjectFilter.ExcludedType, typeof(XObject));

            Assert.Equal(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i].OptionType);
            var typeFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++].Instance);
            Assert.Equal(typeFilter.ExcludedType, typeof(Type));

            Assert.Equal(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i].OptionType);
            var jTokenFilter 
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++].Instance);
            Assert.Equal(jTokenFilter.ExcludedType, typeof(JToken));

            Assert.Equal(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i].OptionType);
            var cancellationTokenFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++].Instance);
            Assert.Equal(cancellationTokenFilter.ExcludedType, typeof(System.Threading.CancellationToken));

            Assert.Equal(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i].OptionType);
            var formFileFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++].Instance);
            Assert.Equal(formFileFilter.ExcludedType, typeof(Http.IFormFile));

            Assert.Equal(
                typeof(DefaultTypeBasedExcludeFilter),
                mvcOptions.ValidationExcludeFilters[i].OptionType);
            var formCollectionFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++].Instance);
            Assert.Equal(formCollectionFilter.ExcludedType, typeof(Http.IFormCollection));

            Assert.Equal(typeof(DefaultTypeNameBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i].OptionType);
            var xmlNodeFilter = 
                     Assert.IsType<DefaultTypeNameBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++].Instance);
            Assert.Equal(xmlNodeFilter.ExcludedTypeName, "System.Xml.XmlNode");
        }
    }
}