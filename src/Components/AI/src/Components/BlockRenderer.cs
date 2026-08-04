// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class BlockRenderer<TBlock> : IComponent, IDisposable where TBlock : ContentBlock
{
    private RenderHandle _renderHandle;
    private bool _initialized;
    private BlockRendererRegistration? _registration;

    [CascadingParameter]
    public MessageListContext ListContext { get; set; } = default!;

    [Parameter]
    public RenderFragment<TBlock>? ChildContent { get; set; }

    [Parameter]
    public Func<TBlock, bool>? When { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (ListContext is null)
        {
            throw new InvalidOperationException(
                "BlockRenderer must be placed inside a MessageList.");
        }

        if (!_initialized)
        {
            _initialized = true;

            _registration = new BlockRendererRegistration
            {
                BlockType = typeof(TBlock),
                // Capture 'this' so the lambda reads the latest When/ChildContent at invocation time
                When = block => block is TBlock typed && (When is null || When(typed)),
                Render = block => ChildContent!((TBlock)block)
            };

            ListContext.AddRegistration(_registration);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_registration is not null)
        {
            ListContext?.RemoveRegistration(_registration);
        }
    }
}
