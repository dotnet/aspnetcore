// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public MvcOptionsSetup() : base(ConfigureMvcOptions)
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder;
        }

        /// <inheritdoc />
        public static void ConfigureMvcOptions(MvcOptions options)
        {
            // Set up ViewEngines
            options.ViewEngines.Add(typeof(RazorViewEngine));

            // Set up ModelBinding
            options.ModelBinders.Add(typeof(BinderTypeBasedModelBinder));
            options.ModelBinders.Add(typeof(ServicesModelBinder));
            options.ModelBinders.Add(typeof(BodyModelBinder));
            options.ModelBinders.Add(new HeaderModelBinder());
            options.ModelBinders.Add(new TypeConverterModelBinder());
            options.ModelBinders.Add(new TypeMatchModelBinder());
            options.ModelBinders.Add(new CancellationTokenModelBinder());
            options.ModelBinders.Add(new ByteArrayModelBinder());
            options.ModelBinders.Add(new FormFileModelBinder());
            options.ModelBinders.Add(new FormCollectionModelBinder());
            options.ModelBinders.Add(typeof(GenericModelBinder));
            options.ModelBinders.Add(new MutableObjectModelBinder());
            options.ModelBinders.Add(new ComplexModelDtoModelBinder());

            // Set up default output formatters.
            options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
            options.OutputFormatters.Add(new StringOutputFormatter());
            options.OutputFormatters.Add(new StreamOutputFormatter());
            options.OutputFormatters.Add(new JsonOutputFormatter());

            // Set up default mapping for json extensions to content type
            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));

            // Set up default input formatters.
            options.InputFormatters.Add(new JsonInputFormatter());

            // Set up ValueProviders
            options.ValueProviderFactories.Add(new RouteValueValueProviderFactory());
            options.ValueProviderFactories.Add(new QueryStringValueProviderFactory());
            options.ValueProviderFactories.Add(new FormValueProviderFactory());

            // Set up validators
            options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider());
            options.ModelValidatorProviders.Add(new DataMemberModelValidatorProvider());

            // Add types to be excluded from Validation
            options.ValidationExcludeFilters.Add(new SimpleTypesExcludeFilter());
            options.ValidationExcludeFilters.Add(typeof(XObject));
            options.ValidationExcludeFilters.Add(typeof(Type));
            options.ValidationExcludeFilters.Add(typeof(JToken));

            // Any 'known' types that we bind should be marked as excluded from validation.
            options.ValidationExcludeFilters.Add(typeof(System.Threading.CancellationToken));
            options.ValidationExcludeFilters.Add(typeof(Http.IFormFile));
            options.ValidationExcludeFilters.Add(typeof(Http.IFormCollection));

            options.ValidationExcludeFilters.Add(typeFullName: "System.Xml.XmlNode");
        }
    }
}