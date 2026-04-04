// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorUnitedApp.Client;

public partial class ThrowExceptionsChild
{
    protected override void OnInitialized()
    {
        throw new Exception(nameof(OnInitialized));
    }

    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(500);
        throw new Exception(nameof(OnInitializedAsync));
    }

    private void Save()
    {
        throw new Exception("No internet connection!");
    }
}
