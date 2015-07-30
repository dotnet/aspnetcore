// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public static class MvcOptionsExtensions
    {
        /// <summary>
        /// Adds <see cref="Microsoft.AspNet.Mvc.Formatters.Xml.XmlDataContractSerializerInputFormatter"/> and 
        /// <see cref="Microsoft.AspNet.Mvc.Formatters.Xml.XmlDataContractSerializerOutputFormatter"/> to the
        /// input and output formatter collections respectively.
        /// </summary>
        /// <param name="options">The MvcOptions</param>
        public static void AddXmlDataContractSerializerFormatter([NotNull] this MvcOptions options)
        {
            MvcXmlDataContractSerializerMvcOptionsSetup.ConfigureMvc(options);
        }
    }
}