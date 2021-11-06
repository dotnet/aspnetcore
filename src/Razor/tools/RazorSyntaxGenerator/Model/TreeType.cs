// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Serialization;

namespace RazorSyntaxGenerator;

public class TreeType
{
    [XmlAttribute]
    public string Name;

    [XmlAttribute]
    public string Base;

    [XmlElement]
    public Comment TypeComment;

    [XmlElement]
    public Comment FactoryComment;
}
