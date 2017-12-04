// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal
{
    /// <summary>
    /// A <see cref="IConfigureOptions{TOptions}"/> implementation which will add the
    /// data contract serializer formatters to <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcXmlDataContractSerializerMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcXmlDataContractSerializerMvcOptionsSetup"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public MvcXmlDataContractSerializerMvcOptionsSetup(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Adds the data contract serializer formatters to <see cref="MvcOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public void Configure(MvcOptions options)
        {
            options.ModelMetadataDetailsProviders.Add(new DataMemberRequiredBindingMetadataProvider());

            options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter(_loggerFactory));
            options.InputFormatters.Add(new XmlDataContractSerializerInputFormatter(options));

            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider("System.Xml.Linq.XObject"));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider("System.Xml.XmlNode"));
        }
    }
}
