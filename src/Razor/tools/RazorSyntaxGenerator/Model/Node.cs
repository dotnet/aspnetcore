// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace RazorSyntaxGenerator;

public class Node : TreeType
{
    [XmlAttribute]
    public string Root;

    [XmlAttribute]
    public string Errors;

    [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
    public List<Kind> Kinds;

    [XmlElement(ElementName = "Field", Type = typeof(Field))]
    public List<Field> Fields;
}
