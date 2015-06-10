// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class CoreMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public CoreMvcOptionsSetup()
            : base(ConfigureMvc)
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder;
        }

        public static void ConfigureMvc(MvcOptions options)
        {
            // Set up ModelBinding
            options.ModelBinders.Add(new BinderTypeBasedModelBinder());
            options.ModelBinders.Add(new ServicesModelBinder());
            options.ModelBinders.Add(new BodyModelBinder());
            options.ModelBinders.Add(new HeaderModelBinder());
            options.ModelBinders.Add(new TypeConverterModelBinder());
            options.ModelBinders.Add(new TypeMatchModelBinder());
            options.ModelBinders.Add(new CancellationTokenModelBinder());
            options.ModelBinders.Add(new ByteArrayModelBinder());
            options.ModelBinders.Add(new FormFileModelBinder());
            options.ModelBinders.Add(new FormCollectionModelBinder());
            options.ModelBinders.Add(new GenericModelBinder());
            options.ModelBinders.Add(new MutableObjectModelBinder());
            options.ModelBinders.Add(new ComplexModelDtoModelBinder());

            // Set up default output formatters.
            options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
            options.OutputFormatters.Add(new StringOutputFormatter());
            options.OutputFormatters.Add(new StreamOutputFormatter());

            // Set up ValueProviders
            options.ValueProviderFactories.Add(new RouteValueValueProviderFactory());
            options.ValueProviderFactories.Add(new QueryStringValueProviderFactory());
            options.ValueProviderFactories.Add(new FormValueProviderFactory());

            // Set up metadata providers
            options.ModelMetadataDetailsProviders.Add(new DefaultBindingMetadataProvider());
            options.ModelMetadataDetailsProviders.Add(new DefaultValidationMetadataProvider());

            // Set up validators
            options.ModelValidatorProviders.Add(new DefaultModelValidatorProvider());

            // Add types to be excluded from Validation
            options.ValidationExcludeFilters.Add(new SimpleTypesExcludeFilter());
            options.ValidationExcludeFilters.Add(typeof(Type));

            // Any 'known' types that we bind should be marked as excluded from validation.
            options.ValidationExcludeFilters.Add(typeof(System.Threading.CancellationToken));
            options.ValidationExcludeFilters.Add(typeof(Http.IFormFile));
            options.ValidationExcludeFilters.Add(typeof(Http.IFormCollection));
        }
    }
}