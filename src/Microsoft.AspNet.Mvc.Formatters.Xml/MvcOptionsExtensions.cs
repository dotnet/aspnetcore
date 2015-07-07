// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.AspNet.Mvc.Formatters.Xml;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public static class MvcOptionsExtensions
    {
        /// <summary>
        /// Adds <see cref="XmlDataContractSerializerInputFormatter"/> and 
        /// <see cref="XmlDataContractSerializerOutputFormatter"/> to the input and output formatter 
        /// collections respectively.
        /// </summary>
        /// <param name="options">The MvcOptions</param>
        public static void AddXmlDataContractSerializerFormatter([NotNull] this MvcOptions options)
        {
            options.ModelMetadataDetailsProviders.Add(new DataMemberRequiredBindingMetadataProvider());

            options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
            options.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());

            options.ValidationExcludeFilters.Add(typeof(XObject));
            options.ValidationExcludeFilters.Add(typeFullName: "System.Xml.XmlNode");
        }
    }
}