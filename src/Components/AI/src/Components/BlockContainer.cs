// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

internal class BlockContainer : IDisposable
{
    private readonly ContentBlock _block;
    private readonly MessageListContext _listContext;
    private readonly ContentBlockChangedSubscription _changedSub;

    internal BlockContainer(ContentBlock block, MessageListContext listContext, Action requestRender)
    {
        _block = block;
        _listContext = listContext;
        _changedSub = block.OnChanged(requestRender);
    }

    internal void RenderTo(Rendering.RenderTreeBuilder builder, int seq)
    {
        var fragment = _listContext.RenderBlock(_block);
        builder.AddContent(seq, fragment);
    }

    public void Dispose()
    {
        _changedSub.Dispose();
    }
}
