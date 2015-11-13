// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class MvcOptionsSetupTest
    {
        [Fact]
        public void Setup_SetsUpViewEngines()
        {
            // Arrange & Act
            var options = GetOptions<MvcViewOptions>(AddDnxServices);

            // Assert
            var viewEngine = Assert.Single(options.ViewEngines);
            Assert.IsType<RazorViewEngine>(viewEngine);
        }

        [Fact]
        public void Setup_SetsUpModelBinders()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            var i = 0;
            Assert.Equal(11, options.ModelBinders.Count);
            Assert.IsType(typeof(BinderTypeBasedModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(ServicesModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(BodyModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(HeaderModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(SimpleTypeModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(CancellationTokenModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(ByteArrayModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(FormFileModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(FormCollectionModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(GenericModelBinder), options.ModelBinders[i++]);
            Assert.IsType(typeof(MutableObjectModelBinder), options.ModelBinders[i++]);
        }

        [Fact]
        public void Setup_SetsUpValueProviders()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            var valueProviders = options.ValueProviderFactories;
            Assert.Equal(4, valueProviders.Count);
            Assert.IsType<RouteValueValueProviderFactory>(valueProviders[0]);
            Assert.IsType<QueryStringValueProviderFactory>(valueProviders[1]);
            Assert.IsType<FormValueProviderFactory>(valueProviders[2]);
            Assert.IsType<JQueryFormValueProviderFactory>(valueProviders[3]);
        }

        [Fact]
        public void Setup_SetsUpOutputFormatters()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            Assert.Equal(4, options.OutputFormatters.Count);
            Assert.IsType<HttpNoContentOutputFormatter>(options.OutputFormatters[0]);
            Assert.IsType<StringOutputFormatter>(options.OutputFormatters[1]);
            Assert.IsType<StreamOutputFormatter>(options.OutputFormatters[2]);
            Assert.IsType<JsonOutputFormatter>(options.OutputFormatters[3]);
        }

        [Fact]
        public void Setup_SetsUpInputFormatters()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            Assert.Equal(2, options.InputFormatters.Count);
            Assert.IsType<JsonInputFormatter>(options.InputFormatters[0]);
            Assert.IsType<JsonPatchInputFormatter>(options.InputFormatters[1]);
        }

        [Fact]
        public void Setup_SetsUpModelValidatorProviders()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            Assert.Equal(2, options.ModelValidatorProviders.Count);
            Assert.IsType<DefaultModelValidatorProvider>(options.ModelValidatorProviders[0]);
            Assert.IsType<DataAnnotationsModelValidatorProvider>(options.ModelValidatorProviders[1]);
        }

        [Fact]
        public void Setup_SetsUpClientModelValidatorProviders()
        {
            // Arrange & Act
            var options = GetOptions<MvcViewOptions>(AddDnxServices);

            // Assert
            Assert.Equal(3, options.ClientModelValidatorProviders.Count);
            Assert.IsType<DefaultClientModelValidatorProvider>(options.ClientModelValidatorProviders[0]);
            Assert.IsType<DataAnnotationsClientModelValidatorProvider>(options.ClientModelValidatorProviders[1]);
            Assert.IsType<NumericClientModelValidatorProvider>(options.ClientModelValidatorProviders[2]);
        }

        [Fact]
        public void Setup_IgnoresAcceptHeaderHavingWildCardMediaAndSubMediaTypes()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            Assert.False(options.RespectBrowserAcceptHeader);
        }

        [Fact]
        public void Setup_SetsUpExcludeFromValidationDelegates()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>(services =>
            {
                var builder = new MvcCoreBuilder(services);
                builder.AddXmlDataContractSerializerFormatters();
            });

            // Assert
            Assert.Equal(8, options.ValidationExcludeFilters.Count);
            var i = 0;

            // Verify if the delegates registered by default exclude the given types.
            Assert.IsType(typeof(SimpleTypesExcludeFilter), options.ValidationExcludeFilters[i++]);

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), options.ValidationExcludeFilters[i]);
            var typeFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(options.ValidationExcludeFilters[i++]);
            Assert.Equal(typeFilter.ExcludedType, typeof(Type));

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), options.ValidationExcludeFilters[i]);
            var cancellationTokenFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(options.ValidationExcludeFilters[i++]);
            Assert.Equal(cancellationTokenFilter.ExcludedType, typeof(System.Threading.CancellationToken));

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), options.ValidationExcludeFilters[i]);
            var formFileFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(options.ValidationExcludeFilters[i++]);
            Assert.Equal(formFileFilter.ExcludedType, typeof(Http.IFormFile));

            Assert.IsType(
                typeof(DefaultTypeBasedExcludeFilter),
                options.ValidationExcludeFilters[i]);
            var formCollectionFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(options.ValidationExcludeFilters[i++]);
            Assert.Equal(formCollectionFilter.ExcludedType, typeof(Http.IFormCollection));

            Assert.IsType(typeof(DefaultTypeBasedExcludeFilter), options.ValidationExcludeFilters[i]);
            var jTokenFilter
                = Assert.IsType<DefaultTypeBasedExcludeFilter>(options.ValidationExcludeFilters[i++]);
            Assert.Equal(jTokenFilter.ExcludedType, typeof(JToken));

            Assert.IsType(typeof(DefaultTypeNameBasedExcludeFilter), options.ValidationExcludeFilters[i]);
            var xObjectFilter
                = Assert.IsType<DefaultTypeNameBasedExcludeFilter>(options.ValidationExcludeFilters[i++]);
            Assert.Equal(xObjectFilter.ExcludedTypeName, typeof(XObject).FullName);

            Assert.IsType(typeof(DefaultTypeNameBasedExcludeFilter), options.ValidationExcludeFilters[i]);
            var xmlNodeFilter =
                     Assert.IsType<DefaultTypeNameBasedExcludeFilter>(options.ValidationExcludeFilters[i++]);
            Assert.Equal(xmlNodeFilter.ExcludedTypeName, "System.Xml.XmlNode");
        }

        [Fact]
        public void Setup_JsonFormattersUseSerializerSettings()
        {
            // Arrange
            var services = GetServiceProvider();

            // Act
            var options = services.GetRequiredService<IOptions<MvcOptions>>().Value;
            var jsonOptions = services.GetRequiredService<IOptions<MvcJsonOptions>>().Value;

            // Assert
            var jsonInputFormatters = options.InputFormatters.OfType<JsonInputFormatter>();
            foreach (var jsonInputFormatter in jsonInputFormatters)
            {
                Assert.Same(jsonOptions.SerializerSettings, jsonInputFormatter.SerializerSettings);
            }

            var jsonOuputFormatters = options.OutputFormatters.OfType<JsonOutputFormatter>();
            foreach (var jsonOuputFormatter in jsonOuputFormatters)
            {
                Assert.Same(jsonOptions.SerializerSettings, jsonOuputFormatter.SerializerSettings);
            }
        }

        private static T GetOptions<T>(Action<IServiceCollection> action = null)
            where T : class, new()
        {
            var serviceProvider = GetServiceProvider(action);
            return serviceProvider.GetRequiredService<IOptions<T>>().Value;
        }

        private static IServiceProvider GetServiceProvider(Action<IServiceCollection> action = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMvc();

            if (action != null)
            {
                action(serviceCollection);
            }

            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }

        private static void AddDnxServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(Mock.Of<ILibraryManager>());
            serviceCollection.AddSingleton(Mock.Of<ILibraryExporter>());
            serviceCollection.AddSingleton(Mock.Of<ICompilerOptionsProvider>());
            serviceCollection.AddSingleton(Mock.Of<IAssemblyLoadContextAccessor>());
            var applicationEnvironment = new Mock<IApplicationEnvironment>();

            // ApplicationBasePath is used to set up a PhysicalFileProvider which requires
            // a real directory.
            applicationEnvironment.SetupGet(e => e.ApplicationBasePath)
                .Returns(Directory.GetCurrentDirectory());

            serviceCollection.AddSingleton(applicationEnvironment.Object);
        }
    }
}
