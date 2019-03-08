// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace RazorSyntaxGenerator
{
    public class AbstractNode : TreeType
    {
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        public List<Field> Fields;
    }
}
