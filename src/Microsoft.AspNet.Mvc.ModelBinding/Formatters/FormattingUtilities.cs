// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Contains methods which are used by input formatters.
    /// </summary>
    public static class FormattingUtilities
    {
        public static readonly int DefaultMaxDepth = 32;

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

        /// Internal because ContentTypeHeaderValue is internal.
        internal static Encoding SelectCharacterEncoding(IList<Encoding> supportedEncodings,
            ContentTypeHeaderValue contentType, Type callerType)
        {
            if (contentType != null)
            {
                // Find encoding based on content type charset parameter
                var charset = contentType.CharSet;
                if (!string.IsNullOrWhiteSpace(contentType.CharSet))
                {
                    for (var i = 0; i < supportedEncodings.Count; i++)
                    {
                        var supportedEncoding = supportedEncodings[i];
                        if (string.Equals(charset, supportedEncoding.WebName, StringComparison.OrdinalIgnoreCase))
                        {
                            return supportedEncoding;
                        }
                    }
                }
            }

            if (supportedEncodings.Count > 0)
            {
                return supportedEncodings[0];
            }

            // No supported encoding was found so there is no way for us to start reading.
            throw new InvalidOperationException(Resources.FormatMediaTypeFormatterNoEncoding(callerType.FullName));
        }
    }
}