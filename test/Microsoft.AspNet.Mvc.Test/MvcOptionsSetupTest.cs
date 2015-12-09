// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNet.Hosting;
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
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;

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
        public void Setup_SetsUpMetadataDetailsProviders()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>(services =>
            {
                var builder = new MvcCoreBuilder(services);
                builder.AddXmlDataContractSerializerFormatters();
            });

            // Assert
            var providers = options.ModelMetadataDetailsProviders;
            Assert.Equal(12, providers.Count);
            var i = 0;

            Assert.IsType<DefaultBindingMetadataProvider>(providers[i++]);
            Assert.IsType<DefaultValidationMetadataProvider>(providers[i++]);

            var excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal(typeof(Type), excludeFilter.Type);

            excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal(typeof(Uri), excludeFilter.Type);

            excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal(typeof(CancellationToken), excludeFilter.Type);

            excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal(typeof(IFormFile), excludeFilter.Type);

            excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal(typeof(IFormCollection), excludeFilter.Type);

            Assert.IsType<DataAnnotationsMetadataProvider>(providers[i++]);

            excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal(typeof(JToken), excludeFilter.Type);

            Assert.IsType<DataMemberRequiredBindingMetadataProvider>(providers[i++]);

            excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal(typeof(XObject).FullName, excludeFilter.FullTypeName);

            excludeFilter = Assert.IsType<ValidationExcludeFilter>(providers[i++]);
            Assert.Equal("System.Xml.XmlNode", excludeFilter.FullTypeName);
        }

        [Fact]
        public void Setup_JsonFormattersUseSerializerSettings()
        {
            // Arrange
            var services = GetServiceProvider(s =>
            {
                s.AddTransient<ILoggerFactory, LoggerFactory>();
            });

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
            serviceCollection.AddTransient<ILoggerFactory, LoggerFactory>();
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
            serviceCollection.AddSingleton(Mock.Of<IHostingEnvironment>());
            var applicationEnvironment = new Mock<IApplicationEnvironment>();

            // ApplicationBasePath is used to set up a PhysicalFileProvider which requires
            // a real directory.
            applicationEnvironment.SetupGet(e => e.ApplicationBasePath)
                .Returns(Directory.GetCurrentDirectory());

            serviceCollection.AddSingleton(applicationEnvironment.Object);
        }
    }
}
