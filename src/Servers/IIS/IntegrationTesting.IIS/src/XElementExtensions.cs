// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS;

public static class XElementExtensions
{
    public static XElement RequiredElement(this XElement element, string name)
    {
        var existing = element.Element(name);
        if (existing == null)
        {
            throw new InvalidOperationException($"Element with name {name} not found in {element}");
        }

        return existing;
    }

    public static XElement GetOrAdd(this XElement element, string name)
    {
        var existing = element.Element(name);
        if (existing == null)
        {
            existing = new XElement(name);
            element.Add(existing);
        }

        return existing;
    }

    public static XElement GetOrAdd(this XElement element, string name, string attribute, string attributeValue)
    {
        var existing = element.Elements(name).FirstOrDefault(e => e.Attribute(attribute)?.Value == attributeValue);
        if (existing == null)
        {
            existing = new XElement(name, new XAttribute(attribute, attributeValue));
            element.Add(existing);
        }

        return existing;
    }

    public static XElement AddAndGetInnerElement(this XElement element, string name, string attribute, string attributeValue)
    {
        var innerElement = new XElement(name, new XAttribute(attribute, attributeValue));
        element.Add(innerElement);

        return innerElement;
    }
}
