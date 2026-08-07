// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class ComponentBlockRenderer<TBlock, TRenderer> : IComponent, IDisposable
    where TBlock : ContentBlock
    where TRenderer : IComponent
{
    private RenderHandle _renderHandle;
    private bool _initialized;
    private BlockRendererRegistration? _registration;

    [CascadingParameter]
    public MessageListContext ListContext { get; set; } = default!;

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
                "ComponentBlockRenderer must be placed inside a MessageList.");
        }

        if (!_initialized)
        {
            _initialized = true;

            _registration = new BlockRendererRegistration
            {
                BlockType = typeof(TBlock),
                When = block => block is TBlock typed && (When is null || When(typed)),
                Render = block => builder =>
                {
                    builder.OpenComponent<TRenderer>(0);
                    builder.AddComponentParameter(1, "Block", (TBlock)block);
                    builder.CloseComponent();
                }
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
