// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
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
            Assert.Collection(options.ModelBinders,
                binder => Assert.IsType<BinderTypeBasedModelBinder>(binder),
                binder => Assert.IsType<ServicesModelBinder>(binder),
                binder => Assert.IsType<BodyModelBinder>(binder),
                binder => Assert.IsType<HeaderModelBinder>(binder),
                binder => Assert.IsType<SimpleTypeModelBinder>(binder),
                binder => Assert.IsType<CancellationTokenModelBinder>(binder),
                binder => Assert.IsType<ByteArrayModelBinder>(binder),
                binder => Assert.IsType<FormFileModelBinder>(binder),
                binder => Assert.IsType<FormCollectionModelBinder>(binder),
                binder => Assert.IsType<GenericModelBinder>(binder),
                binder => Assert.IsType<MutableObjectModelBinder>(binder));
        }

        [Fact]
        public void Setup_SetsUpValueProviders()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            var valueProviders = options.ValueProviderFactories;
            Assert.Collection(valueProviders,
                provider => Assert.IsType<FormValueProviderFactory>(provider),
                provider => Assert.IsType<RouteValueProviderFactory>(provider),
                provider => Assert.IsType<QueryStringValueProviderFactory>(provider),
                provider => Assert.IsType<JQueryFormValueProviderFactory>(provider));
        }

        [Fact]
        public void Setup_SetsUpOutputFormatters()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            Assert.Collection(options.OutputFormatters,
                formatter => Assert.IsType<HttpNoContentOutputFormatter>(formatter),
                formatter => Assert.IsType<StringOutputFormatter>(formatter),
                formatter => Assert.IsType<StreamOutputFormatter>(formatter),
                formatter => Assert.IsType<JsonOutputFormatter>(formatter));
        }

        [Fact]
        public void Setup_SetsUpInputFormatters()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            Assert.Collection(options.InputFormatters,
                formatter => Assert.IsType<JsonInputFormatter>(formatter),
                formatter => Assert.IsType<JsonPatchInputFormatter>(formatter));
        }

        [Fact]
        public void Setup_SetsUpModelValidatorProviders()
        {
            // Arrange & Act
            var options = GetOptions<MvcOptions>();

            // Assert
            Assert.Collection(options.ModelValidatorProviders,
                validator => Assert.IsType<DefaultModelValidatorProvider>(validator),
                validator => Assert.IsType<DataAnnotationsModelValidatorProvider>(validator));
        }

        [Fact]
        public void Setup_SetsUpClientModelValidatorProviders()
        {
            // Arrange & Act
            var options = GetOptions<MvcViewOptions>(AddDnxServices);

            // Assert
            Assert.Collection(options.ClientModelValidatorProviders,
                validator => Assert.IsType<DefaultClientModelValidatorProvider>(validator),
                validator => Assert.IsType<DataAnnotationsClientModelValidatorProvider>(validator),
                validator => Assert.IsType<NumericClientModelValidatorProvider>(validator));
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
            Assert.Collection(providers,
                provider => Assert.IsType<DefaultBindingMetadataProvider>(provider),
                provider => Assert.IsType<DefaultValidationMetadataProvider>(provider),
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(Type), excludeFilter.Type);
                },
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(Uri), excludeFilter.Type);
                },
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(CancellationToken), excludeFilter.Type);
                },
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(IFormFile), excludeFilter.Type);
                },
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(IFormCollection), excludeFilter.Type);
                },
                provider => Assert.IsType<DataAnnotationsMetadataProvider>(provider),
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(JToken), excludeFilter.Type);
                },
                provider => Assert.IsType<DataMemberRequiredBindingMetadataProvider>(provider),
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(XObject).FullName, excludeFilter.FullTypeName);
                },
                provider =>
                {
                    var excludeFilter = Assert.IsType<ValidationExcludeFilter>(provider);
                    Assert.Equal(typeof(XmlNode).FullName, excludeFilter.FullTypeName);
                });
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
