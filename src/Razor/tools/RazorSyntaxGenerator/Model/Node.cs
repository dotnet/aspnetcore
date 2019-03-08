// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace RazorSyntaxGenerator
{
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
}
