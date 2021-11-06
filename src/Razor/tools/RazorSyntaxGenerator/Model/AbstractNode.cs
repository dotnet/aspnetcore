// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace RazorSyntaxGenerator;

public class AbstractNode : TreeType
{
    [XmlElement(ElementName = "Field", Type = typeof(Field))]
    public List<Field> Fields;
}
