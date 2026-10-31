// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal sealed class InvalidUrlRewriteFormatException : FormatException
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
