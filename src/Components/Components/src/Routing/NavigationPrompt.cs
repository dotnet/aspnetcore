// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// A component that displays a browser prompt when the user navigates away from the page.
/// </summary>
public class NavigationPrompt : IComponent, IAsyncDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets message to be displayed when the user navigates away from the page.
    /// </summary>
    /// <remarks>
    /// Some browsers will not display this message for external navigations.
    /// For more info, see <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event#compatibility_notes"/>.
    /// </remarks>
    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether the prompt will only display for external navigations.
    /// </summary>
    [Parameter]
    public bool ExternalNavigationsOnly { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
    }

    async Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (string.IsNullOrEmpty(Message))
        {
            throw new InvalidOperationException($"{nameof(NavigationPrompt)} requires a non-empty value for the parameter {nameof(Message)}.");
        }

        await NavigationManager.EnableNavigationPromptAsync(Message, ExternalNavigationsOnly);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await NavigationManager.DisableNavigationPromptAsync();
    }
}
