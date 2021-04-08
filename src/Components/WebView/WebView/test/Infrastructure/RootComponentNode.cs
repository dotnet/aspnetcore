// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebView.Document
{
    internal class RootComponentNode : ComponentNode
    {
        public RootComponentNode(int componentId, string selector) : base(componentId)
        {
            Selector = selector;
        }

        public string Selector { get; }
    }
}
