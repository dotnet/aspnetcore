// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Xml;
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
            options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());

            options.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());
        }
    }
}