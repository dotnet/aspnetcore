// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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
            // Set up default error messages
            var messageProvider = options.ModelBindingMessageProvider;
            messageProvider.MissingBindRequiredValueAccessor = Resources.FormatModelBinding_MissingBindRequiredMember;
            messageProvider.MissingKeyOrValueAccessor = Resources.FormatKeyValuePair_BothKeyAndValueMustBePresent;
            messageProvider.ValueMustNotBeNullAccessor = Resources.FormatModelBinding_NullValueNotValid;
            messageProvider.AttemptedValueIsInvalidAccessor = Resources.FormatModelState_AttemptedValueIsInvalid;
            messageProvider.UnknownValueIsInvalidAccessor = Resources.FormatModelState_UnknownValueIsInvalid;
            messageProvider.ValueIsInvalidAccessor = Resources.FormatHtmlGeneration_ValueIsInvalid;
            messageProvider.ValueMustBeANumberAccessor = Resources.FormatHtmlGeneration_ValueMustBeNumber;

            // Set up ModelBinding
            options.ModelBinderProviders.Add(new BinderTypeModelBinderProvider());
            options.ModelBinderProviders.Add(new ServicesModelBinderProvider());
            options.ModelBinderProviders.Add(new BodyModelBinderProvider(_readerFactory));
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
            options.ModelMetadataDetailsProviders.Add(new DefaultBindingMetadataProvider(messageProvider));
            options.ModelMetadataDetailsProviders.Add(new DefaultValidationMetadataProvider());

            // Set up validators
            options.ModelValidatorProviders.Add(new DefaultModelValidatorProvider());

            // Add types to be excluded from Validation
            options.ModelMetadataDetailsProviders.Add(new ValidationExcludeFilter(typeof(Type)));
            options.ModelMetadataDetailsProviders.Add(new ValidationExcludeFilter(typeof(Uri)));
            options.ModelMetadataDetailsProviders.Add(new ValidationExcludeFilter(typeof(CancellationToken)));
            options.ModelMetadataDetailsProviders.Add(new ValidationExcludeFilter(typeof(IFormFile)));
            options.ModelMetadataDetailsProviders.Add(new ValidationExcludeFilter(typeof(IFormCollection)));
        }
    }
}