// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Handles CSRF protection for the logout endpoint.
    /// </summary>
    public class SignOutSessionStateManager
    {
        private readonly IJSRuntime _jsRuntime;
        private static readonly JsonSerializerOptions _serializationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        public SignOutSessionStateManager(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

        /// <summary>
        /// Sets up some state in session storage to allow for logouts from within the <see cref="RemoteAuthenticationDefaults.LogoutPath"/> page.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that completes when the state has been saved to session storage.</returns>
        public virtual ValueTask SetSignOutState()
        {
            return _jsRuntime.InvokeVoidAsync(
                "sessionStorage.setItem",
                "Microsoft.AspNetCore.Components.WebAssembly.Authentication.SignOutState",
                JsonSerializer.Serialize(SignOutState.Instance, _serializationOptions));
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

            return JsonSerializer.Deserialize<SignOutState>(result, _serializationOptions);
        }

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
}
