// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcCoreMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly IHttpRequestStreamReaderFactory _readerFactory;

        public MvcCoreMvcOptionsSetup(IHttpRequestStreamReaderFactory readerFactory)
        {
            if (readerFactory == null)
            {
                throw new ArgumentNullException(nameof(readerFactory));
            }

            _readerFactory = readerFactory;
        }

        public void Configure(MvcOptions options)
        {
            // Set up ModelBinding
            options.ModelBinderProviders.Add(new BinderTypeModelBinderProvider());
            options.ModelBinderProviders.Add(new ServicesModelBinderProvider());
            options.ModelBinderProviders.Add(new BodyModelBinderProvider(options.InputFormatters, _readerFactory));
            options.ModelBinderProviders.Add(new HeaderModelBinderProvider());
            options.ModelBinderProviders.Add(new SimpleTypeModelBinderProvider());
            options.ModelBinderProviders.Add(new CancellationTokenModelBinderProvider());
            options.ModelBinderProviders.Add(new ByteArrayModelBinderProvider());
            options.ModelBinderProviders.Add(new FormFileModelBinderProvider());
            options.ModelBinderProviders.Add(new FormCollectionModelBinderProvider());
            options.ModelBinderProviders.Add(new KeyValuePairModelBinderProvider());
            options.ModelBinderProviders.Add(new DictionaryModelBinderProvider());
            options.ModelBinderProviders.Add(new ArrayModelBinderProvider());
            options.ModelBinderProviders.Add(new CollectionModelBinderProvider());
            options.ModelBinderProviders.Add(new ComplexTypeModelBinderProvider());

            // Set up filters
            options.Filters.Add(new UnsupportedContentTypeFilter());

            // Set up default output formatters.
            options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
            options.OutputFormatters.Add(new StringOutputFormatter());
            options.OutputFormatters.Add(new StreamOutputFormatter());

            // Set up ValueProviders
            options.ValueProviderFactories.Add(new FormValueProviderFactory());
            options.ValueProviderFactories.Add(new RouteValueProviderFactory());
            options.ValueProviderFactories.Add(new QueryStringValueProviderFactory());
            options.ValueProviderFactories.Add(new JQueryFormValueProviderFactory());

            // Set up metadata providers

            // Don't bind the Type class by default as it's expensive. A user can override this behavior
            // by altering the collection of providers.
            options.ModelMetadataDetailsProviders.Add(new ExcludeBindingMetadataProvider(typeof(Type)));

            options.ModelMetadataDetailsProviders.Add(new DefaultBindingMetadataProvider());
            options.ModelMetadataDetailsProviders.Add(new DefaultValidationMetadataProvider());

            // Set up validators
            options.ModelValidatorProviders.Add(new DefaultModelValidatorProvider());

            // Add types to be excluded from Validation
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Type)));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Uri)));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(CancellationToken)));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IFormFile)));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IFormCollection)));
        }
    }
}