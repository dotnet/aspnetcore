// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal
{
    /// <summary>
    /// A <see cref="IConfigureOptions{TOptions}"/> implementation which will add the
    /// XML serializer formatters to <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcXmlSerializerMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcXmlSerializerMvcOptionsSetup"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public MvcXmlSerializerMvcOptionsSetup(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Adds the XML serializer formatters to <see cref="MvcOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public void Configure(MvcOptions options)
        {
            options.OutputFormatters.Add(new XmlSerializerOutputFormatter(_loggerFactory));
            options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
        }
    }
}
