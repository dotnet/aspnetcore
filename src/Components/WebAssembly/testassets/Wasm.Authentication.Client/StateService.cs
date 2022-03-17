// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Authentication.Client;

public class StateService
{
    private string _state;

    public string GetCurrentState() => _state ??= Guid.NewGuid().ToString();

    public void RestoreCurrentState(string state) => _state = state;
}
