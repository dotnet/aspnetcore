// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// After navigating from one page to another, sets focus to an element
/// matching a CSS selector. This can be used to build an accessible
/// navigation system compatible with screen readers.
/// </summary>
public class FocusOnNavigate : ComponentBase
{
    private Type? _lastNavigatedPageType = typeof(NonMatchingType);
    private bool _focusAfterRender;

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the route data. This can be obtained from an enclosing
    /// <see cref="Router"/> component.
    /// </summary>
    [Parameter] public RouteData? RouteData { get; set; } // TODO: [EditorRequired]

    /// <summary>
    /// Gets or sets a CSS selector describing the element to be focused after
    /// navigation between pages.
    /// </summary>
    [Parameter] public string? Selector { get; set; } // TODO: [EditorRequired]

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (RouteData is null)
        {
            throw new InvalidOperationException($"{nameof(FocusOnNavigate)} requires a non-null value for the parameter '{nameof(RouteData)}'.");
        }

        if (string.IsNullOrWhiteSpace(Selector))
        {
            throw new InvalidOperationException($"{nameof(FocusOnNavigate)} requires a nonempty value for the parameter '{nameof(Selector)}'.");
        }

        // We focus whenever the page type changes, including to or from 'null'
        if (RouteData!.PageType != _lastNavigatedPageType)
        {
            _lastNavigatedPageType = RouteData!.PageType;
            _focusAfterRender = true;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusAfterRender)
        {
            _focusAfterRender = false;
            await JSRuntime.InvokeVoidAsync(DomWrapperInterop.FocusBySelector, Selector);
        }
    }

    // On the first render, we always want to consider the page type changed, even if it's null.
    // So we need some other non-null type to compare with it.
    private sealed class NonMatchingType { }
}
