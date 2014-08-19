// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Contains methods which are used by input formatters.
    /// </summary>
    public static class FormattingUtilities
    {
        public static readonly int DefaultMaxDepth = 32;

#if NET45
        public static readonly XsdDataContractExporter XsdDataContractExporter = new XsdDataContractExporter();
#endif

        /// <summary>
        /// Gets the default Reader Quotas for XmlReader.
        /// </summary>
        /// <returns>XmlReaderQuotas with default values</returns>
        public static XmlDictionaryReaderQuotas GetDefaultXmlReaderQuotas()
        {
#if NET45
            return new XmlDictionaryReaderQuotas()
            {
                MaxArrayLength = Int32.MaxValue,
                MaxBytesPerRead = Int32.MaxValue,
                MaxDepth = DefaultMaxDepth,
                MaxNameTableCharCount = Int32.MaxValue,
                MaxStringContentLength = Int32.MaxValue
            };
#else
            return XmlDictionaryReaderQuotas.Max;
#endif
        }
    }
}