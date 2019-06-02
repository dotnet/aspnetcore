// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Ignitor
{
    internal class ElementNode : ContainerNode
    {
        private readonly string _elementName;

        public ElementNode(string elementName)
        {
            _elementName = elementName;
        }
    }
}
