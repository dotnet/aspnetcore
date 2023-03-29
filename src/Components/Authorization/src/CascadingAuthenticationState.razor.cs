// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// Cascading authentication state
/// </summary>
public partial class CascadingAuthenticationState : ComponentBase, IDisposable
{
    private Task<AuthenticationState>? _currentAuthenticationStateTask;

    /// <summary>
    /// The content to which the authentication state should be provided.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        _currentAuthenticationStateTask = AuthenticationStateProvider.GetAuthenticationStateAsync();
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> newAuthStateTask)
    {
        _ = InvokeAsync(() =>
        {
            _currentAuthenticationStateTask = newAuthStateTask;
            StateHasChanged();
        });
    }

    void IDisposable.Dispose()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
