// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

internal class NavigationPrompt : IComponent, IHandleLocationChanging, IDisposable
{
    private bool _externalNavigationsOnly;

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

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (string.IsNullOrEmpty(Message))
        {
            throw new InvalidOperationException($"{nameof(NavigationPrompt)} requires a non-empty value for the parameter {nameof(Message)}.");
        }

        if (_externalNavigationsOnly != ExternalNavigationsOnly)
        {
            _externalNavigationsOnly = ExternalNavigationsOnly;

            if (_externalNavigationsOnly)
            {
                NavigationManager.RemoveLocationChangingHandler(this);
            }
            else
            {
                NavigationManager.AddLocationChangingHandler(this);
            }
        }

        return Task.CompletedTask;
    }

    ValueTask IHandleLocationChanging.OnLocationChanging(LocationChangingContext context)
    {
        // TODO
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (!_externalNavigationsOnly)
        {
            NavigationManager.RemoveLocationChangingHandler(this);
        }
    }
}
