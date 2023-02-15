// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class MvcOptionsSetupTest
{
    [Fact]
    public void Setup_SetsUpViewEngines()
    {
        // Arrange & Act
        var options = GetOptions<MvcViewOptions>(AddViewEngineOptionsServices);

        // Assert
        var viewEngine = Assert.Single(options.ViewEngines);
        Assert.IsType<RazorViewEngine>(viewEngine);
    }

    [Fact]
    public void Setup_SetsUpModelBinderProviders()
    {
        // Arrange & Act
        var options = GetOptions<MvcOptions>();

        // Assert
        Assert.Collection(
            options.ModelBinderProviders,
            binder => Assert.IsType<BinderTypeModelBinderProvider>(binder),
            binder => Assert.IsType<ServicesModelBinderProvider>(binder),
            binder => Assert.IsType<BodyModelBinderProvider>(binder),
            binder => Assert.IsType<HeaderModelBinderProvider>(binder),
            binder => Assert.IsType<FloatingPointTypeModelBinderProvider>(binder),
            binder => Assert.IsType<EnumTypeModelBinderProvider>(binder),
            binder => Assert.IsType<DateTimeModelBinderProvider>(binder),
            binder => Assert.IsType<SimpleTypeModelBinderProvider>(binder),
            binder => Assert.IsType<TryParseModelBinderProvider>(binder),
            binder => Assert.IsType<CancellationTokenModelBinderProvider>(binder),
            binder => Assert.IsType<ByteArrayModelBinderProvider>(binder),
            binder => Assert.IsType<FormFileModelBinderProvider>(binder),
            binder => Assert.IsType<FormCollectionModelBinderProvider>(binder),
            binder => Assert.IsType<KeyValuePairModelBinderProvider>(binder),
            binder => Assert.IsType<DictionaryModelBinderProvider>(binder),
            binder => Assert.IsType<ArrayModelBinderProvider>(binder),
            binder => Assert.IsType<CollectionModelBinderProvider>(binder),
            binder => Assert.IsType<ComplexObjectModelBinderProvider>(binder));
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
            provider => Assert.IsType<JQueryFormValueProviderFactory>(provider),
            provider => Assert.IsType<FormFileValueProviderFactory>(provider));
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
            formatter => Assert.IsType<SystemTextJsonOutputFormatter>(formatter));
    }

    [Fact]
    public void Setup_SetsUpInputFormatters()
    {
        // Arrange & Act
        var options = GetOptions<MvcOptions>();

        // Assert
        Assert.Collection(
            options.InputFormatters,
            formatter => Assert.IsType<SystemTextJsonInputFormatter>(formatter));
    }

    [Fact]
    public void Setup_SetsUpFormatterMapping()
    {
        // Arrange & Act
        var options = GetOptions<MvcOptions>();

        // Assert
        var mapping = options.FormatterMappings.GetMediaTypeMappingForFormat("json");
        Assert.Equal("application/json", mapping);
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
        var options = GetOptions<MvcViewOptions>(AddViewEngineOptionsServices);

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
            var builder = new MvcCoreBuilder(services, new ApplicationPartManager());
            builder.AddXmlDataContractSerializerFormatters();
        });

        // Assert
        var providers = options.ModelMetadataDetailsProviders;
        Assert.Collection(providers,
            provider => Assert.IsType<ExcludeBindingMetadataProvider>(provider),
            provider => Assert.IsType<DefaultBindingMetadataProvider>(provider),
            provider => Assert.IsType<DefaultValidationMetadataProvider>(provider),
            provider =>
            {
                var specialParameter = Assert.IsType<BindingSourceMetadataProvider>(provider);
                Assert.Equal(typeof(CancellationToken), specialParameter.Type);
                Assert.Equal(BindingSource.Special, specialParameter.BindingSource);
            },
            provider =>
            {
                var formFileParameter = Assert.IsType<BindingSourceMetadataProvider>(provider);
                Assert.Equal(typeof(IFormFile), formFileParameter.Type);
                Assert.Equal(BindingSource.FormFile, formFileParameter.BindingSource);
            },
            provider =>
            {
                var formCollectionParameter = Assert.IsType<BindingSourceMetadataProvider>(provider);
                Assert.Equal(typeof(IFormCollection), formCollectionParameter.Type);
                Assert.Equal(BindingSource.FormFile, formCollectionParameter.BindingSource);
            },
            provider =>
            {
                var formFileParameter = Assert.IsType<BindingSourceMetadataProvider>(provider);
                Assert.Equal(typeof(IFormFileCollection), formFileParameter.Type);
                Assert.Equal(BindingSource.FormFile, formFileParameter.BindingSource);
            },
            provider =>
            {
                var formFileParameter = Assert.IsType<BindingSourceMetadataProvider>(provider);
                Assert.Equal(typeof(IEnumerable<IFormFile>), formFileParameter.Type);
                Assert.Equal(BindingSource.FormFile, formFileParameter.BindingSource);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(Type), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(Delegate), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(MethodInfo), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(MemberInfo), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(ParameterInfo), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(Assembly), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(Uri), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(CancellationToken), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(IFormFile), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(IFormCollection), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(IFormFileCollection), excludeFilter.Type);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(Stream), excludeFilter.Type);
            },
            provider => Assert.IsType<DataAnnotationsMetadataProvider>(provider),
            provider => Assert.IsType<DataMemberRequiredBindingMetadataProvider>(provider),
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(XObject).FullName, excludeFilter.FullTypeName);
            },
            provider =>
            {
                var excludeFilter = Assert.IsType<SuppressChildValidationMetadataProvider>(provider);
                Assert.Equal(typeof(XmlNode).FullName, excludeFilter.FullTypeName);
            },
            provider => Assert.IsType<HasValidatorsValidationMetadataProvider>(provider));
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
        serviceCollection.AddSingleton(new ApplicationPartManager());
        var diagnosticListener = new DiagnosticListener("Microsoft.AspNetCore.Mvc");
        serviceCollection.AddSingleton<DiagnosticSource>(diagnosticListener);
        serviceCollection.AddSingleton<DiagnosticListener>(diagnosticListener);
        serviceCollection.AddMvc();
        serviceCollection
            .AddTransient<ILoggerFactory, LoggerFactory>()
            .AddTransient(typeof(ILogger<>), typeof(Logger<>));

        if (action != null)
        {
            action(serviceCollection);
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    private static void AddViewEngineOptionsServices(IServiceCollection serviceCollection)
    {
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.SetupGet(e => e.ApplicationName)
            .Returns(typeof(MvcOptionsSetupTest).Assembly.GetName().Name);

        hostingEnvironment.SetupGet(e => e.ContentRootFileProvider)
            .Returns(Mock.Of<IFileProvider>());

        serviceCollection.AddSingleton(hostingEnvironment.Object);
    }
}
