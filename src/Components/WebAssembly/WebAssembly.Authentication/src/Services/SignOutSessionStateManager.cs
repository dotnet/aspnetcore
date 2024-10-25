// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Handles CSRF protection for the logout endpoint.
/// </summary>
[Obsolete("Use 'Microsoft.AspNetCore.Components.Webassembly.Authentication.NavigationManagerExtensions.NavigateToLogout' instead.")]
public class SignOutSessionStateManager
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Initialize a new instance of <see cref="SignOutSessionStateManager"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    public SignOutSessionStateManager(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    /// <summary>
    /// Sets up some state in session storage to allow for logouts from within the <see cref="RemoteAuthenticationDefaults.LogoutPath"/> page.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes when the state has been saved to session storage.</returns>
    [DynamicDependency(JsonSerialized, typeof(SignOutState))]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The correct members will be preserved by the above DynamicDependency.")]
    // This should use JSON source generation
    public virtual ValueTask SetSignOutState()
    {
        return _jsRuntime.InvokeVoidAsync(
            "sessionStorage.setItem",
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication.SignOutState",
            JsonSerializer.Serialize(SignOutState.Instance, JsonSerializerOptions.Web));
    }

    /// <summary>
    /// Validates the existence of some state previously setup by <see cref="SetSignOutState"/> in session storage to allow
    /// logouts from within the <see cref="RemoteAuthenticationDefaults.LogoutPath"/> page.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the state has been validated and indicates the validity of the state.</returns>
    public virtual async Task<bool> ValidateSignOutState()
    {
        var state = await GetSignOutState();
        if (state.Local)
        {
            await ClearSignOutState();
            return true;
        }

        return false;
    }

    private async ValueTask<SignOutState> GetSignOutState()
    {
        var result = await _jsRuntime.InvokeAsync<string>(
            "sessionStorage.getItem",
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication.SignOutState");
        if (result == null)
        {
            return default;
        }

        return DeserializeSignOutState(result);
    }

    [DynamicDependency(JsonSerialized, typeof(SignOutState))]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The correct members will be preserved by the above DynamicDependency.")]
    // This should use JSON source generation
    private static SignOutState DeserializeSignOutState(string result) => JsonSerializer.Deserialize<SignOutState>(result, JsonSerializerOptions.Web);

    private ValueTask ClearSignOutState()
    {
        return _jsRuntime.InvokeVoidAsync(
            "sessionStorage.removeItem",
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication.SignOutState");
    }

    private struct SignOutState
    {
        public static readonly SignOutState Instance = new SignOutState { Local = true };

        public bool Local { get; set; }
    }
}
