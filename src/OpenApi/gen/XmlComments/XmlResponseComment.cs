// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.XPath;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;

internal sealed class XmlResponseComment
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string? Example { get; set; }

    public static List<XmlResponseComment> GetXmlResponseCommentList(XPathNavigator navigator, string xpath)
    {
        var iterator = navigator.Select(xpath);
        var result = new List<XmlResponseComment>();
        if (iterator == null)
        {
            return result;
        }

        foreach (XPathNavigator nav in iterator)
        {
            var code = nav.GetAttribute("code", string.Empty);

            if (!string.IsNullOrEmpty(code))
            {
                var description = nav.InnerXml.TrimEachLine();
                var example = nav.GetAttribute("example", string.Empty);
                result.Add(new XmlResponseComment { Code = code, Description = description, Example = example });
            }
        }

        return result;
    }
}
