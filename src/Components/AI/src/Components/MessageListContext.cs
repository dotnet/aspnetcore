// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Resolves the markup used to render a block inside a <see cref="MessageList"/>. Renderers
/// registered with <see cref="BlockRenderer{TBlock}"/> take precedence over the built-in
/// rendering.
/// </summary>
public class MessageListContext
{
    private readonly List<BlockRendererRegistration> _registrations = new();

    /// <summary>
    /// Returns the markup for a block.
    /// </summary>
    /// <param name="block">The block to render.</param>
    /// <returns>The markup that renders <paramref name="block"/>.</returns>
    public RenderFragment RenderBlock(ContentBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        foreach (var reg in _registrations)
        {
            if (reg.BlockType.IsAssignableFrom(block.GetType())
                && (reg.When is null || reg.When(block)))
            {
                return reg.Render(block);
            }
        }

        return builder =>
        {
            if (block is RichContentBlock rich)
            {
                var role = block.Role == ChatRole.User ? "user" : "assistant";
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", $"sc-ai-message sc-ai-message--{role}");
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", "sc-ai-message__bubble");
                builder.OpenElement(4, "div");
                var contentClass = block.LifecycleState == BlockLifecycleState.Active
                    ? "sc-ai-message__content sc-ai-message__content--streaming"
                    : "sc-ai-message__content";
                builder.AddAttribute(5, "class", contentClass);
                builder.AddContent(6, rich.RawText);
                builder.CloseElement(); // content div
                builder.CloseElement(); // bubble div
                builder.CloseElement(); // message div
            }
            else
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "sc-ai-unknown-block");
                builder.AddContent(2, block.GetType().Name);
                builder.CloseElement();
            }
        };
    }

    internal Action? OnRegistrationsChanged { get; set; }

    internal void AddRegistration(BlockRendererRegistration registration)
    {
        _registrations.Add(registration);
        OnRegistrationsChanged?.Invoke();
    }

    internal void RemoveRegistration(BlockRendererRegistration registration)
    {
        _registrations.Remove(registration);
        OnRegistrationsChanged?.Invoke();
    }
}
