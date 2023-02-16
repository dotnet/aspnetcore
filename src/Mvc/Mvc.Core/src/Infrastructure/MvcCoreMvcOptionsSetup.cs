// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Sets up default options for <see cref="MvcOptions"/>.
/// </summary>
internal sealed class MvcCoreMvcOptionsSetup : IConfigureOptions<MvcOptions>, IPostConfigureOptions<MvcOptions>
{
    private readonly IHttpRequestStreamReaderFactory _readerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<JsonOptions> _jsonOptions;

    public MvcCoreMvcOptionsSetup(IHttpRequestStreamReaderFactory readerFactory)
        : this(readerFactory, NullLoggerFactory.Instance, Options.Create(new JsonOptions()))
    {
    }

    public MvcCoreMvcOptionsSetup(IHttpRequestStreamReaderFactory readerFactory, ILoggerFactory loggerFactory, IOptions<JsonOptions> jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(readerFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        _readerFactory = readerFactory;
        _loggerFactory = loggerFactory;
        _jsonOptions = jsonOptions;
    }

    public void Configure(MvcOptions options)
    {
        // Set up ModelBinding
        options.ModelBinderProviders.Add(new BinderTypeModelBinderProvider());
        options.ModelBinderProviders.Add(new ServicesModelBinderProvider());
        options.ModelBinderProviders.Add(new BodyModelBinderProvider(options.InputFormatters, _readerFactory, _loggerFactory, options));
        options.ModelBinderProviders.Add(new HeaderModelBinderProvider());
        options.ModelBinderProviders.Add(new FloatingPointTypeModelBinderProvider());
        options.ModelBinderProviders.Add(new EnumTypeModelBinderProvider(options));
        options.ModelBinderProviders.Add(new DateTimeModelBinderProvider());
        options.ModelBinderProviders.Add(new SimpleTypeModelBinderProvider());
        options.ModelBinderProviders.Add(new TryParseModelBinderProvider());
        options.ModelBinderProviders.Add(new CancellationTokenModelBinderProvider());
        options.ModelBinderProviders.Add(new ByteArrayModelBinderProvider());
        options.ModelBinderProviders.Add(new FormFileModelBinderProvider());
        options.ModelBinderProviders.Add(new FormCollectionModelBinderProvider());
        options.ModelBinderProviders.Add(new KeyValuePairModelBinderProvider());
        options.ModelBinderProviders.Add(new DictionaryModelBinderProvider());
        options.ModelBinderProviders.Add(new ArrayModelBinderProvider());
        options.ModelBinderProviders.Add(new CollectionModelBinderProvider());
        options.ModelBinderProviders.Add(new ComplexObjectModelBinderProvider());

        // Set up filters
        options.Filters.Add(new UnsupportedContentTypeFilter());

        // Set up default input formatters.
        options.InputFormatters.Add(new SystemTextJsonInputFormatter(_jsonOptions.Value, _loggerFactory.CreateLogger<SystemTextJsonInputFormatter>()));

        // Media type formatter mappings for JSON
        options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValues.ApplicationJson);

        // Set up default output formatters.
        options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
        options.OutputFormatters.Add(new StringOutputFormatter());
        options.OutputFormatters.Add(new StreamOutputFormatter());

        var jsonOutputFormatter = SystemTextJsonOutputFormatter.CreateFormatter(_jsonOptions.Value);
        options.OutputFormatters.Add(jsonOutputFormatter);

        // Set up ValueProviders
        options.ValueProviderFactories.Add(new FormValueProviderFactory());
        options.ValueProviderFactories.Add(new RouteValueProviderFactory());
        options.ValueProviderFactories.Add(new QueryStringValueProviderFactory());
        options.ValueProviderFactories.Add(new JQueryFormValueProviderFactory());
        options.ValueProviderFactories.Add(new FormFileValueProviderFactory());

        // Set up metadata providers
        ConfigureAdditionalModelMetadataDetailsProviders(options.ModelMetadataDetailsProviders);

        // Set up validators
        options.ModelValidatorProviders.Add(new DefaultModelValidatorProvider());
    }

    public void PostConfigure(string? name, MvcOptions options)
    {
        // HasValidatorsValidationMetadataProvider uses the results of other ValidationMetadataProvider to determine if a model requires
        // validation. It is imperative that this executes later than all other metadata provider. We'll register it as part of PostConfigure.
        // This should ensure it appears later than all of the details provider registered by MVC and user configured details provider registered
        // as part of ConfigureOptions.
        options.ModelMetadataDetailsProviders.Add(new HasValidatorsValidationMetadataProvider(options.ModelValidatorProviders));
    }

    internal static void ConfigureAdditionalModelMetadataDetailsProviders(IList<IMetadataDetailsProvider> modelMetadataDetailsProviders)
    {
        // Don't bind the Type class by default as it's expensive. A user can override this behavior
        // by altering the collection of providers.
        modelMetadataDetailsProviders.Add(new ExcludeBindingMetadataProvider(typeof(Type)));

        modelMetadataDetailsProviders.Add(new DefaultBindingMetadataProvider());
        modelMetadataDetailsProviders.Add(new DefaultValidationMetadataProvider());

        modelMetadataDetailsProviders.Add(new BindingSourceMetadataProvider(typeof(CancellationToken), BindingSource.Special));
        modelMetadataDetailsProviders.Add(new BindingSourceMetadataProvider(typeof(IFormFile), BindingSource.FormFile));
        modelMetadataDetailsProviders.Add(new BindingSourceMetadataProvider(typeof(IFormCollection), BindingSource.FormFile));
        modelMetadataDetailsProviders.Add(new BindingSourceMetadataProvider(typeof(IFormFileCollection), BindingSource.FormFile));
        modelMetadataDetailsProviders.Add(new BindingSourceMetadataProvider(typeof(IEnumerable<IFormFile>), BindingSource.FormFile));

        // Add types to be excluded from Validation
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Type)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Delegate)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(MethodInfo)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(MemberInfo)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(ParameterInfo)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Assembly)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Uri)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(CancellationToken)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IFormFile)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IFormCollection)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IFormFileCollection)));
        modelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Stream)));
    }
}
