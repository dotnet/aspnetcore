// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Forms
{
    /*
     * Currently, anything directly inside a <CascadingValue> can't receive events, because
     * CascadingValue doesn't implement IHandleEvent. This is a manifestation of the event
     * routing bug - the event should really be routed to the component whose markup contains
     * the ChildContent we passed to CascadingValue.
     *
     * This workaround is semi-effective. It avoids the "cannot handle events" exception, but
     * doesn't cause the correct target component to re-render, so the target still has to
     * call StateHasChanged manually when it shouldn't have to.
     *
     * TODO: Once the underlying issue is fixed, remove this class and its usage entirely.
     */

    internal class TemporaryEventRoutingWorkaround : ComponentBase
    {
        [Parameter] RenderFragment ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            builder.AddContent(0, ChildContent);
        }
    }
}
