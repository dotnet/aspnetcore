// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

public class NavigationPrompt : IComponent, IAsyncDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = default!;

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
