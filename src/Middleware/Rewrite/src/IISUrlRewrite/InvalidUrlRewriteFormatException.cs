// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal class InvalidUrlRewriteFormatException : FormatException
    {
        public int LineNumber { get; }
        public int LinePosition { get; }

        public InvalidUrlRewriteFormatException(XElement element, string message)
            : base(FormatMessage(element, message))
        {
        }

        public InvalidUrlRewriteFormatException(XElement element, string message, Exception innerException)
            : base(FormatMessage(element, message), innerException)
        {
            var xmlLineInfo = (IXmlLineInfo)element;
            LineNumber = xmlLineInfo.LineNumber;
            LinePosition = xmlLineInfo.LinePosition;
        }

        private static string FormatMessage(XElement element, string message)
        {
            var xmlLineInfo = (IXmlLineInfo)element;
            return Resources.FormatError_UrlRewriteParseError(message, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
        }
    }
}