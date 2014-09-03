// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcOptionsSetup : IOptionsSetup<MvcOptions>
    {
        /// <inheritdoc />
        /// <remarks>Order is -1 to allow MvcOptionsSetup to run before a user call to SetupOptions.</remarks>
        public int Order
        {
            get { return -1; }
        }

        /// <inheritdoc />
        public void Setup(MvcOptions options)
        {
            // Set up ViewEngines
            options.ViewEngines.Add(typeof(RazorViewEngine));

            // Set up ModelBinding
            options.ModelBinders.Add(new TypeConverterModelBinder());
            options.ModelBinders.Add(new TypeMatchModelBinder());
            options.ModelBinders.Add(new ByteArrayModelBinder());
            options.ModelBinders.Add(typeof(GenericModelBinder));
            options.ModelBinders.Add(new MutableObjectModelBinder());
            options.ModelBinders.Add(new ComplexModelDtoModelBinder());

            // Set up default output formatters.
            options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
            options.OutputFormatters.Add(new TextPlainFormatter());
            options.OutputFormatters.Add(new JsonOutputFormatter(JsonOutputFormatter.CreateDefaultSettings(),
                                         indent: false));
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
        }
    }
}