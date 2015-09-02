// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="ConfigureOptions{TOptions}"/> implementation which will add the
    /// data contract serializer formatters to <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcXmlDataContractSerializerMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        /// <summary>
        /// Creates a new instance of <see cref="MvcXmlDataContractSerializerMvcOptionsSetup"/>.
        /// </summary>
        public MvcXmlDataContractSerializerMvcOptionsSetup()
            : base(ConfigureMvc)
        {
        }

        /// <summary>
        /// Adds the data contract serializer formatters to <see cref="MvcOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public static void ConfigureMvc(MvcOptions options)
        {
            options.ModelMetadataDetailsProviders.Add(new DataMemberRequiredBindingMetadataProvider());

            options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
            options.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());

            options.ValidationExcludeFilters.Add(typeFullName: "System.Xml.Linq.XObject");
            options.ValidationExcludeFilters.Add(typeFullName: "System.Xml.XmlNode");
        }
    }
}
