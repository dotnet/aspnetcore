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
            Assert.Equal(typeof(RazorViewEngine), mvcOptions.ViewEngines[0].ViewEngineType);
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
            Assert.IsType(typeof(BinderTypeBasedModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(ServicesModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(BodyModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(HeaderModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(TypeConverterModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(TypeMatchModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(CancellationTokenModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(ByteArrayModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(FormFileModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(FormCollectionModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(GenericModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(MutableObjectModelBinder), mvcOptions.ModelBinders[i++]);
            Assert.IsType(typeof(ComplexModelDtoModelBinder), mvcOptions.ModelBinders[i++]);
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
            Assert.IsType<RouteValueValueProviderFactory>(valueProviders[0]);
            Assert.IsType<QueryStringValueProviderFactory>(valueProviders[1]);
            Assert.IsType<FormValueProviderFactory>(valueProviders[2]);
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
            Assert.IsType<HttpNoContentOutputFormatter>(mvcOptions.OutputFormatters[0]);
            Assert.IsType<StringOutputFormatter>(mvcOptions.OutputFormatters[1]);
            Assert.IsType<StreamOutputFormatter>(mvcOptions.OutputFormatters[2]);
            Assert.IsType<JsonOutputFormatter>(mvcOptions.OutputFormatters[3]);
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
            Assert.Equal(2, mvcOptions.InputFormatters.Count);
            Assert.IsType<JsonInputFormatter>(mvcOptions.InputFormatters[0]);
            Assert.IsType<JsonPatchInputFormatter>(mvcOptions.InputFormatters[1]);
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
            Assert.IsType<DefaultModelValidatorProvider>(mvcOptions.ModelValidatorProviders[0]);
            Assert.IsType<DataAnnotationsModelValidatorProvider>(mvcOptions.ModelValidatorProviders[1]);
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
            Assert.IsType(typeof(SimpleTypesExcludeFilter), mvcOptions.ValidationExcludeFilters[i++]);
            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i]);
            var xObjectFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++]);
            Assert.Equal(xObjectFilter.ExcludedType, typeof(XObject));

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i]);
            var typeFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++]);
            Assert.Equal(typeFilter.ExcludedType, typeof(Type));

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i]);
            var jTokenFilter 
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++]);
            Assert.Equal(jTokenFilter.ExcludedType, typeof(JToken));

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i]);
            var cancellationTokenFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++]);
            Assert.Equal(cancellationTokenFilter.ExcludedType, typeof(System.Threading.CancellationToken));

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i]);
            var formFileFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++]);
            Assert.Equal(formFileFilter.ExcludedType, typeof(Http.IFormFile));

            Assert.IsType(
                typeof(DefaultTypeBasedExcludeFilter),
                mvcOptions.ValidationExcludeFilters[i]);
            var formCollectionFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++]);
            Assert.Equal(formCollectionFilter.ExcludedType, typeof(Http.IFormCollection));

            Assert.IsType(typeof(DefaultTypeNameBasedExcludeFilter), mvcOptions.ValidationExcludeFilters[i]);
            var xmlNodeFilter = 
                     Assert.IsType<DefaultTypeNameBasedExcludeFilter>(mvcOptions.ValidationExcludeFilters[i++]);
            Assert.Equal(xmlNodeFilter.ExcludedTypeName, "System.Xml.XmlNode");
        }
    }
}