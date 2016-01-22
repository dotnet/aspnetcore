// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal
{
    /// <summary>
    /// A <see cref="ConfigureOptions{TOptions}"/> implementation which will add the
    /// XML serializer formatters to <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcXmlSerializerMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        /// <summary>
        /// Creates a new <see cref="MvcXmlSerializerMvcOptionsSetup"/>.
        /// </summary>
        public MvcXmlSerializerMvcOptionsSetup()
            : base(ConfigureMvc)
        {
        }

        /// <summary>
        /// Adds the XML serializer formatters to <see cref="MvcOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public static void ConfigureMvc(MvcOptions options)
        {
            options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
            options.InputFormatters.Add(new XmlSerializerInputFormatter());
        }
    }
}
