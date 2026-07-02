// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorUnitedApp.Client;

public class ErrorBoundaryTest(IJSRuntime jSRuntime) : ErrorBoundaryBase
{
    protected override async Task OnErrorAsync(Exception exception)
    {
        await jSRuntime.InvokeVoidAsync("alert", $"An unknown error occurred: {exception.Message}");
    }
}
