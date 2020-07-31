// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    public class VirtualizeTest
    {
        // TODO: Functional tests.
        [Fact]
        public void Virtualize_ThrowsWhenGivenNonPositiveItemSize()
        {
            var rootComponent = new VirtualizeTestHostcomponent
            {
                InnerContent = BuildVirtualize(
                    i => builder => { },
                    null,
                    context => builder => { },
                    0f,
                    null,
                    new List<int>())
            };

            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(rootComponent);

            var ex = Assert.Throws<InvalidOperationException>(() => testRenderer.RenderRootComponent(componentId));
        }

        public RenderFragment BuildVirtualize<TItem>(
            RenderFragment<TItem> childContent,
            RenderFragment<TItem> item,
            RenderFragment<PlaceholderContext> placeholder,
            float itemSize,
            ItemsProviderDelegate<TItem> itemsProvider,
            ICollection<TItem> items)
            => builder =>
        {
            builder.OpenComponent<Virtualize<TItem>>(0);
            builder.AddAttribute(1, "ChildContent", childContent);
            builder.AddAttribute(2, "Item", item);
            builder.AddAttribute(3, "Placeholder", placeholder);
            builder.AddAttribute(4, "ItemSize", itemSize);
            builder.AddAttribute(5, "ItemsProvider", itemsProvider);
            builder.AddAttribute(6, "Items", items);
            builder.CloseComponent();
        };

        private class VirtualizeTestHostcomponent : AutoRenderComponent
        {
            public RenderFragment InnerContent { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", "overflow: auto; height: 800px;");
                builder.AddAttribute(2, "ChildContent", InnerContent);
                builder.CloseElement();
            }
        }
    }
}
