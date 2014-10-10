// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.OptionsModel;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public MvcOptionsSetup() : base(ConfigureMvc)
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder;
        }

        /// <inheritdoc />
        public static void ConfigureMvc(MvcOptions options)
        {
            // Set up ViewEngines
            options.ViewEngines.Add(typeof(RazorViewEngine));

            // Set up ModelBinding
            options.ModelBinders.Add(typeof(BodyModelBinder));
            options.ModelBinders.Add(new TypeConverterModelBinder());
            options.ModelBinders.Add(new TypeMatchModelBinder());
            options.ModelBinders.Add(new CancellationTokenModelBinder());
            options.ModelBinders.Add(new ByteArrayModelBinder());
            options.ModelBinders.Add(typeof(GenericModelBinder));
            options.ModelBinders.Add(new MutableObjectModelBinder());
            options.ModelBinders.Add(new ComplexModelDtoModelBinder());

            // Set up default output formatters.
            options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
            options.OutputFormatters.Add(new TextPlainFormatter());
            options.OutputFormatters.Add(new JsonOutputFormatter());
            options.OutputFormatters.Add(
                new XmlDataContractSerializerOutputFormatter(XmlOutputFormatter.GetDefaultXmlWriterSettings()));

            // Set up default input formatters.
            options.InputFormatters.Add(new JsonInputFormatter());
            options.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());

            // Set up ValueProviders
            options.ValueProviderFactories.Add(new RouteValueValueProviderFactory());
            options.ValueProviderFactories.Add(new QueryStringValueProviderFactory());
            options.ValueProviderFactories.Add(new FormValueProviderFactory());

            // Set up validators
            options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider());
            options.ModelValidatorProviders.Add(new DataMemberModelValidatorProvider());

            // Add types to be excluded from Validation
            options.ExcludeFromValidationDelegates.Add(typeof(XmlNode));
            options.ExcludeFromValidationDelegates.Add(typeof(XObject));
            options.ExcludeFromValidationDelegates.Add(typeof(Type));
            options.ExcludeFromValidationDelegates.Add(typeof(byte[]));
            options.ExcludeFromValidationDelegates.Add(typeof(JToken));
        }
    }
}