// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.XPath;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;

internal sealed class XmlParameterComment
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Example { get; set; }
    public bool? Deprecated { get; set; }

    public static List<XmlParameterComment> GetXmlParameterListComment(XPathNavigator navigator, string xpath)
    {
        var iterator = navigator.Select(xpath);
        var result = new List<XmlParameterComment>();
        if (iterator == null)
        {
            return result;
        }
        foreach (XPathNavigator nav in iterator)
        {
            var name = nav.GetAttribute("name", string.Empty);

            if (!string.IsNullOrEmpty(name))
            {
                var description = nav.InnerXml.TrimEachLine();
                var example = nav.GetAttribute("example", string.Empty);
                var deprecated = nav.GetAttribute("deprecated", string.Empty);
                result.Add(new XmlParameterComment { Name = name, Description = description, Example = example, Deprecated = deprecated == "true" });
            }
        }

        return result;
    }
}
