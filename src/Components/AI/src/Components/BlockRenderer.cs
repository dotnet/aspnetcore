// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Registers custom markup for a block type inside a <see cref="MessageList"/>. The most
/// recently registered renderer that matches a block wins.
/// </summary>
/// <typeparam name="TBlock">The block type this renderer handles.</typeparam>
/// <example>
/// <code>
/// &lt;MessageList&gt;
///     &lt;BlockRenderer TBlock="RichContentBlock" Context="block"&gt;
///         &lt;p&gt;@block.RawText&lt;/p&gt;
///     &lt;/BlockRenderer&gt;
/// &lt;/MessageList&gt;
/// </code>
/// </example>
public class BlockRenderer<TBlock> : IComponent, IDisposable where TBlock : ContentBlock
{
    private RenderHandle _renderHandle;
    private bool _initialized;
    private BlockRendererRegistration? _registration;

    /// <summary>
    /// Gets or sets the message list this renderer registers with.
    /// </summary>
    [CascadingParameter]
    public MessageListContext ListContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the markup rendered for a matching block.
    /// </summary>
    [Parameter]
    public RenderFragment<TBlock>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a predicate that narrows which blocks this renderer handles.
    /// </summary>
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

        if (ChildContent is null)
        {
            throw new InvalidOperationException("BlockRenderer requires child content.");
        }

        if (!_initialized)
        {
            _initialized = true;

            _registration = new BlockRendererRegistration
            {
                BlockType = typeof(TBlock),
                // Capture 'this' so the lambda reads the latest When/ChildContent at invocation time
                When = block => block is TBlock typed && (When is null || When(typed)),
                Render = block => ChildContent((TBlock)block)
            };

            ListContext.AddRegistration(_registration);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes this renderer from the message list.
    /// </summary>
    public void Dispose()
    {
        if (_registration is not null)
        {
            ListContext?.RemoveRegistration(_registration);
        }

        GC.SuppressFinalize(this);
    }
}
