// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.Binding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class BaseModelBindingBenchmark
{
    private static MemoryStream _stream;

    private protected ParameterDescriptor _parameter;
    private protected ModelBindingTestContext _testContext;
    private protected ModelStateDictionary _modelState;
    private protected ModelMetadata _metadata;
    private protected IModelBinder _modelBinder;
    private protected CompositeValueProvider _valueProvider;
    private protected ParameterBinder _parameterBinder;
    private protected FormDataReader _formDataReader;
    private protected FormDataMapperOptions _formMapperOptions;

    [GlobalSetup]
    public async Task Setup()
    {
        _parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = GetParameterType()
        };

        // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
        CreateTestContext();

        _modelState = _testContext.ModelState;
        _metadata = GetMetadata(_testContext, _parameter);
        _modelBinder = GetModelBinder(_testContext, _parameter, _metadata);
        _valueProvider = await CompositeValueProvider.CreateAsync(_testContext);
        _parameterBinder = ModelBindingTestHelper.GetParameterBinder(_testContext);

        _formDataReader = new FormDataReader(new FormCollectionReadOnlyDictionary(_testContext.HttpContext.Request.Form), CultureInfo.InvariantCulture);
        _formMapperOptions = new FormDataMapperOptions();
        _formMapperOptions.ResolveConverter<Customer>();
    }

    protected virtual Type GetParameterType()
    {
        throw new NotImplementedException();
    }

    protected virtual void CreateTestContext()
    {
        throw new NotImplementedException();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _stream.Seek(0, SeekOrigin.Begin);
        _testContext.HttpContext.Request.Body = null;
        _testContext.HttpContext.Request.Body = _stream;
        _testContext.HttpContext.Features.Set<IRequestBodyPipeFeature>(new RequestBodyPipeFeature(_testContext.HttpContext));
        _testContext.HttpContext.Features.Set<IFormFeature>(new FormFeature(_testContext.HttpContext.Request));
    }

    public class Customer
    {
        public string CompanyName { get; set; }

        public string ContactName { get; set; }
        public string ContactTitle { get; set; }

        public Address Address { get; set; }

        public string Phone { get; set; }

        public string Fax { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    protected static void SetFormDataContent(HttpRequest request, string content)
    {
        _stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        _stream.Seek(0, SeekOrigin.Begin);
        request.ContentType = "application/x-www-form-urlencoded";
        request.Body = _stream;
    }

    protected virtual ModelBindingTestContext GetTestContext(
        Action<HttpRequest> updateRequest = null,
        Action<MvcOptions> updateOptions = null,
        IModelMetadataProvider metadataProvider = null)
        => ModelBindingTestHelper.GetTestContext(updateRequest, updateOptions, actionDescriptor: null, metadataProvider);

    private ModelMetadata GetMetadata(ModelBindingTestContext context, ParameterDescriptor parameter)
    {
        return context.MetadataProvider.GetMetadataForType(parameter.ParameterType);
    }

    private IModelBinder GetModelBinder(
        ModelBindingTestContext context,
        ParameterDescriptor parameter,
        ModelMetadata metadata)
    {
        var factory = ModelBindingTestHelper.GetModelBinderFactory(
            context.MetadataProvider,
            context.HttpContext.RequestServices);
        var factoryContext = new ModelBinderFactoryContext
        {
            BindingInfo = parameter.BindingInfo,
            CacheToken = parameter,
            Metadata = metadata,
        };

        return factory.CreateBinder(factoryContext);
    }

    public static class ModelBindingTestHelper
    {
        public static ModelBindingTestContext GetTestContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            ControllerActionDescriptor actionDescriptor = null,
            IModelMetadataProvider metadataProvider = null,
            MvcOptions mvcOptions = null)
        {
            var httpContext = GetHttpContext(metadataProvider, updateRequest, updateOptions, mvcOptions);
            var services = httpContext.RequestServices;
            metadataProvider = metadataProvider ?? services.GetRequiredService<IModelMetadataProvider>();
            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            var context = new ModelBindingTestContext
            {
                ActionDescriptor = actionDescriptor ?? new ControllerActionDescriptor(),
                HttpContext = httpContext,
                MetadataProvider = metadataProvider,
                MvcOptions = options.Value,
                RouteData = new RouteData(),
                ValueProviderFactories = new List<IValueProviderFactory>(options.Value.ValueProviderFactories),
            };

            return context;
        }

        public static ParameterBinder GetParameterBinder(
            MvcOptions options = null,
            IModelBinderProvider binderProvider = null)
        {
            if (options == null)
            {
                var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
                return GetParameterBinder(metadataProvider, binderProvider);
            }
            else
            {
                var metadataProvider = TestModelMetadataProvider.CreateProvider(options.ModelMetadataDetailsProviders);
                return GetParameterBinder(metadataProvider, binderProvider, options);
            }
        }

        public static ParameterBinder GetParameterBinder(ModelBindingTestContext testContext)
        {
            return GetParameterBinder(testContext.HttpContext.RequestServices);
        }

        public static ParameterBinder GetParameterBinder(IServiceProvider serviceProvider)
        {
            var metadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
            var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();

            return new ParameterBinder(
                metadataProvider,
                new ModelBinderFactory(metadataProvider, options, serviceProvider),
                new DefaultObjectValidator(
                    metadataProvider,
                    new[] { new CompositeModelValidatorProvider(GetModelValidatorProviders(options)) },
                    options.Value),
                options,
                NullLoggerFactory.Instance);
        }

        public static ParameterBinder GetParameterBinder(
            IModelMetadataProvider metadataProvider,
            IModelBinderProvider binderProvider = null,
            MvcOptions mvcOptions = null,
            ObjectModelValidator validator = null)
        {
            var services = GetServices(metadataProvider, mvcOptions: mvcOptions);
            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            if (binderProvider != null)
            {
                options.Value.ModelBinderProviders.Insert(0, binderProvider);
            }

            validator ??= new DefaultObjectValidator(
                metadataProvider,
                new[] { new CompositeModelValidatorProvider(GetModelValidatorProviders(options)) },
                options.Value);

            return new ParameterBinder(
                metadataProvider,
                new ModelBinderFactory(metadataProvider, options, services),
                validator,
                options,
                NullLoggerFactory.Instance);
        }

        public static IModelBinderFactory GetModelBinderFactory(
            IModelMetadataProvider metadataProvider,
            IServiceProvider services = null)
        {
            if (services == null)
            {
                services = GetServices(metadataProvider);
            }

            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            return new ModelBinderFactory(metadataProvider, options, services);
        }

        public static IObjectModelValidator GetObjectValidator(
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> options = null)
        {
            return new DefaultObjectValidator(
                metadataProvider,
                GetModelValidatorProviders(options),
                options?.Value ?? new MvcOptions());
        }

        private static IList<IModelValidatorProvider> GetModelValidatorProviders(IOptions<MvcOptions> options)
        {
            if (options == null)
            {
                return TestModelValidatorProvider.CreateDefaultProvider().ValidatorProviders;
            }
            else
            {
                return options.Value.ModelValidatorProviders;
            }
        }

        private static HttpContext GetHttpContext(
            IModelMetadataProvider metadataProvider,
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            MvcOptions mvcOptions = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new CancellableRequestLifetimeFeature());
            httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new NonZeroContentLengthRequestBodyDetectionFeature(httpContext));

            updateRequest?.Invoke(httpContext.Request);

            httpContext.RequestServices = GetServices(metadataProvider, updateOptions, mvcOptions);
            return httpContext;
        }

        public static IServiceProvider GetServices(
            IModelMetadataProvider metadataProvider,
            Action<MvcOptions> updateOptions = null,
            MvcOptions mvcOptions = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new ApplicationPartManager());
            if (metadataProvider != null)
            {
                serviceCollection.AddSingleton(metadataProvider);
            }
            else if (updateOptions != null || mvcOptions != null)
            {
                serviceCollection.AddSingleton(services =>
                {
                    var optionsAccessor = services.GetRequiredService<IOptions<MvcOptions>>();
                    return TestModelMetadataProvider.CreateProvider(optionsAccessor.Value.ModelMetadataDetailsProviders);
                });
            }
            else
            {
                serviceCollection.AddSingleton<IModelMetadataProvider>(services =>
                {
                    return TestModelMetadataProvider.CreateDefaultProvider();
                });
            }

            if (mvcOptions != null)
            {
                serviceCollection.AddSingleton(Options.Create(mvcOptions));
            }

            serviceCollection.AddMvc()
                .Services
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                .AddTransient<ILogger<DefaultAuthorizationService>, Logger<DefaultAuthorizationService>>();

            if (updateOptions != null)
            {
                serviceCollection.Configure(updateOptions);
            }

            return serviceCollection.BuildServiceProvider();
        }

        private class CancellableRequestLifetimeFeature : IHttpRequestLifetimeFeature
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public CancellationToken RequestAborted { get => _cts.Token; set => throw new NotImplementedException(); }

            public void Abort()
            {
                _cts.Cancel();
            }
        }

        private class NonZeroContentLengthRequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
        {
            private readonly HttpContext _context;

            public NonZeroContentLengthRequestBodyDetectionFeature(HttpContext context)
            {
                _context = context;
            }

            public bool CanHaveBody => _context.Request.ContentLength != 0;
        }
    }

    public class ModelBindingTestContext : ControllerContext
    {
        public IModelMetadataProvider MetadataProvider { get; set; }

        public MvcOptions MvcOptions { get; set; }

        public T GetService<T>()
        {
            return (T)HttpContext.RequestServices.GetService(typeof(T));
        }
    }

    public class TestModelMetadataProvider : DefaultModelMetadataProvider
    {
        private static DataAnnotationsMetadataProvider CreateDefaultDataAnnotationsProvider(IStringLocalizerFactory stringLocalizerFactory)
        {
            var localizationOptions = Options.Create(new MvcDataAnnotationsLocalizationOptions());
            localizationOptions.Value.DataAnnotationLocalizerProvider = (modelType, localizerFactory) => localizerFactory.Create(modelType);

            return new DataAnnotationsMetadataProvider(new MvcOptions(), localizationOptions, stringLocalizerFactory);
        }

        // Creates a provider with all the defaults - includes data annotations
        public static ModelMetadataProvider CreateDefaultProvider(IStringLocalizerFactory stringLocalizerFactory = null)
        {
            var detailsProviders = new List<IMetadataDetailsProvider>
            {
                new DefaultBindingMetadataProvider(),
                new DefaultValidationMetadataProvider(),
                CreateDefaultDataAnnotationsProvider(stringLocalizerFactory),
                new DataMemberRequiredBindingMetadataProvider(),
            };

            MvcCoreMvcOptionsSetup.ConfigureAdditionalModelMetadataDetailsProviders(detailsProviders);

            var validationProviders = TestModelValidatorProvider.CreateDefaultProvider();
            detailsProviders.Add(new HasValidatorsValidationMetadataProvider(validationProviders.ValidatorProviders));

            var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
            return new DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new MvcOptions()));
        }

        public static IModelMetadataProvider CreateDefaultProvider(IList<IMetadataDetailsProvider> providers)
        {
            var detailsProviders = new List<IMetadataDetailsProvider>()
            {
                new DefaultBindingMetadataProvider(),
                new DefaultValidationMetadataProvider(),
                new DataAnnotationsMetadataProvider(
                    new MvcOptions(),
                    Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                    stringLocalizerFactory: null),
                new DataMemberRequiredBindingMetadataProvider(),
            };

            MvcCoreMvcOptionsSetup.ConfigureAdditionalModelMetadataDetailsProviders(detailsProviders);

            detailsProviders.AddRange(providers);

            var validationProviders = TestModelValidatorProvider.CreateDefaultProvider();
            detailsProviders.Add(new HasValidatorsValidationMetadataProvider(validationProviders.ValidatorProviders));

            var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
            return new DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new MvcOptions()));
        }

        public static IModelMetadataProvider CreateProvider(IList<IMetadataDetailsProvider> providers)
        {
            var detailsProviders = new List<IMetadataDetailsProvider>();
            if (providers != null)
            {
                detailsProviders.AddRange(providers);
            }

            var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
            return new DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new MvcOptions()));
        }

        private readonly TestModelMetadataDetailsProvider _detailsProvider;

        public TestModelMetadataProvider()
            : this(new TestModelMetadataDetailsProvider())
        {
        }

        private TestModelMetadataProvider(TestModelMetadataDetailsProvider detailsProvider)
            : base(
                  new DefaultCompositeMetadataDetailsProvider(new IMetadataDetailsProvider[]
                  {
                      new DefaultBindingMetadataProvider(),
                      new DefaultValidationMetadataProvider(),
                      new DataAnnotationsMetadataProvider(
                          new MvcOptions(),
                          Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                          stringLocalizerFactory: null),
                      detailsProvider
                  }),
                  Options.Create(new MvcOptions()))
        {
            _detailsProvider = detailsProvider;
        }

        public IMetadataBuilder ForType(Type type)
        {
            var key = ModelMetadataIdentity.ForType(type);

            var builder = new MetadataBuilder(key);
            _detailsProvider.Builders.Add(builder);
            return builder;
        }

        public IMetadataBuilder ForType<TModel>()
        {
            return ForType(typeof(TModel));
        }

        public IMetadataBuilder ForProperty(Type containerType, string propertyName)
        {
            var property = containerType.GetRuntimeProperty(propertyName);

            var key = ModelMetadataIdentity.ForProperty(property, property.PropertyType, containerType);

            var builder = new MetadataBuilder(key);
            _detailsProvider.Builders.Add(builder);
            return builder;
        }

        public IMetadataBuilder ForParameter(ParameterInfo parameter)
        {
            var key = ModelMetadataIdentity.ForParameter(parameter);
            var builder = new MetadataBuilder(key);
            _detailsProvider.Builders.Add(builder);

            return builder;
        }

        public IMetadataBuilder ForProperty<TContainer>(string propertyName)
        {
            return ForProperty(typeof(TContainer), propertyName);
        }

        private sealed class TestModelMetadataDetailsProvider :
            IBindingMetadataProvider,
            IDisplayMetadataProvider,
            IValidationMetadataProvider
        {
            public List<MetadataBuilder> Builders { get; } = new List<MetadataBuilder>();

            public void CreateBindingMetadata(BindingMetadataProviderContext context)
            {
                foreach (var builder in Builders)
                {
                    builder.Apply(context);
                }
            }

            public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
            {
                foreach (var builder in Builders)
                {
                    builder.Apply(context);
                }
            }

            public void CreateValidationMetadata(ValidationMetadataProviderContext context)
            {
                foreach (var builder in Builders)
                {
                    builder.Apply(context);
                }
            }
        }

        public interface IMetadataBuilder
        {
            IMetadataBuilder BindingDetails(Action<BindingMetadata> action);

            IMetadataBuilder DisplayDetails(Action<DisplayMetadata> action);

            IMetadataBuilder ValidationDetails(Action<ValidationMetadata> action);
        }

        private sealed class MetadataBuilder : IMetadataBuilder
        {
            private readonly List<Action<BindingMetadata>> _bindingActions = new List<Action<BindingMetadata>>();
            private readonly List<Action<DisplayMetadata>> _displayActions = new List<Action<DisplayMetadata>>();
            private readonly List<Action<ValidationMetadata>> _validationActions = new List<Action<ValidationMetadata>>();

            private readonly ModelMetadataIdentity _key;

            public MetadataBuilder(ModelMetadataIdentity key)
            {
                _key = key;
            }

            public void Apply(BindingMetadataProviderContext context)
            {
                if (_key.Equals(context.Key))
                {
                    foreach (var action in _bindingActions)
                    {
                        action(context.BindingMetadata);
                    }
                }
            }

            public void Apply(DisplayMetadataProviderContext context)
            {
                if (_key.Equals(context.Key))
                {
                    foreach (var action in _displayActions)
                    {
                        action(context.DisplayMetadata);
                    }
                }
            }

            public void Apply(ValidationMetadataProviderContext context)
            {
                if (_key.Equals(context.Key))
                {
                    foreach (var action in _validationActions)
                    {
                        action(context.ValidationMetadata);
                    }
                }
            }

            public IMetadataBuilder BindingDetails(Action<BindingMetadata> action)
            {
                _bindingActions.Add(action);
                return this;
            }

            public IMetadataBuilder DisplayDetails(Action<DisplayMetadata> action)
            {
                _displayActions.Add(action);
                return this;
            }

            public IMetadataBuilder ValidationDetails(Action<ValidationMetadata> action)
            {
                _validationActions.Add(action);
                return this;
            }
        }
    }

    public class DataMemberRequiredBindingMetadataProvider : IBindingMetadataProvider
    {
        /// <inheritdoc />
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // Types cannot be required; only properties can
            if (context.Key.MetadataKind != ModelMetadataKind.Property)
            {
                return;
            }

            if (context.BindingMetadata.IsBindingRequired)
            {
                // This value is already required, no need to look at attributes.
                return;
            }

            var dataMemberAttribute = context
                .PropertyAttributes!
                .OfType<DataMemberAttribute>()
                .FirstOrDefault();
            if (dataMemberAttribute == null || !dataMemberAttribute.IsRequired)
            {
                return;
            }

            // isDataContract == true iff the container type has at least one DataContractAttribute
            var containerType = context.Key.ContainerType;
            var isDataContract = containerType!.IsDefined(typeof(DataContractAttribute));
            if (isDataContract)
            {
                // We don't need to add a validator, just to set IsRequired = true. The validation
                // system will do the right thing.
                context.BindingMetadata.IsBindingRequired = true;
            }
        }
    }

    public class TestModelValidatorProvider : CompositeModelValidatorProvider
    {
        // Creates a provider with all the defaults - includes data annotations
        public static CompositeModelValidatorProvider CreateDefaultProvider(IStringLocalizerFactory stringLocalizerFactory = null)
        {
            var options = Options.Create(new MvcDataAnnotationsLocalizationOptions());
            options.Value.DataAnnotationLocalizerProvider = (modelType, localizerFactory) => localizerFactory.Create(modelType);

            var providers = new IModelValidatorProvider[]
            {
                new DefaultModelValidatorProvider(),
                new DataAnnotationsModelValidatorProvider(
                    new ValidationAttributeAdapterProvider(),
                    options,
                    stringLocalizerFactory)
            };

            return new TestModelValidatorProvider(providers);
        }

        public TestModelValidatorProvider(IList<IModelValidatorProvider> providers)
            : base(providers)
        {
        }
    }

    protected sealed class FormCollectionReadOnlyDictionary : IReadOnlyDictionary<string, StringValues>
    {
        private readonly IFormCollection _form;
        private List<StringValues> _values;

        public FormCollectionReadOnlyDictionary(IFormCollection form)
        {
            _form = form;
        }

        public StringValues this[string key] => _form[key];

        public IEnumerable<string> Keys => _form.Keys;

        public IEnumerable<StringValues> Values => _values ??= MaterializeValues(_form);

        private static List<StringValues> MaterializeValues(IFormCollection form)
        {
            var result = new List<StringValues>(form.Keys.Count);
            foreach (var key in form.Keys)
            {
                result.Add(form[key]);
            }

            return result;
        }

        public int Count => _form.Count;

        public bool ContainsKey(string key)
        {
            return _form.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            return _form.GetEnumerator();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out StringValues value)
        {
            return _form.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _form.GetEnumerator();
        }
    }
}
