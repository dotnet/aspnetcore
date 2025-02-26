// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BlazorServerApp.Data;

public class SamplePersistentService
{
    [SupplyParameterFromPersistentComponentState]
    public string SampleState { get; set; } = "";
}
