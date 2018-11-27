// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A <see cref="IConfigureOptions{TOptions}"/> implementation which will add the
    /// XML serializer formatters to <see cref="MvcOptions"/>.
    /// </summary>
    internal sealed class XmlSerializerMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly MvcXmlOptions _xmlOptions;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="XmlSerializerMvcOptionsSetup"/>.
        /// </summary>
        /// <param name="xmlOptions"><see cref="MvcXmlOptions"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public XmlSerializerMvcOptionsSetup(
            IOptions<MvcXmlOptions> xmlOptions,
            ILoggerFactory loggerFactory)
        {
            _xmlOptions = xmlOptions?.Value ?? throw new ArgumentNullException(nameof(xmlOptions));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Adds the XML serializer formatters to <see cref="MvcOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public void Configure(MvcOptions options)
        {
            // Do not override any user mapping
            var key = "xml";
            var mapping = options.FormatterMappings.GetMediaTypeMappingForFormat(key);
            if (string.IsNullOrEmpty(mapping))
            {
                options.FormatterMappings.SetMediaTypeMappingForFormat(
                    key,
                    MediaTypeHeaderValues.ApplicationXml);
            }

            var inputFormatter = new XmlSerializerInputFormatter(options);
            inputFormatter.WrapperProviderFactories.Add(new ProblemDetailsWrapperProviderFactory(_xmlOptions));
            options.InputFormatters.Add(inputFormatter);

            var outputFormatter = new XmlSerializerOutputFormatter(_loggerFactory);
            outputFormatter.WrapperProviderFactories.Add(new ProblemDetailsWrapperProviderFactory(_xmlOptions));
            options.OutputFormatters.Add(outputFormatter);

        }
    }
}
