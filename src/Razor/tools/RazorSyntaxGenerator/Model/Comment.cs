// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Serialization;

namespace RazorSyntaxGenerator;

public class Comment
{
    [XmlAnyElement]
    public XmlElement[] Body;
}
