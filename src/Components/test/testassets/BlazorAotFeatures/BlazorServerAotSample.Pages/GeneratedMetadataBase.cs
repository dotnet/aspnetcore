// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BlazorServerAotSample.Pages;

public abstract class GeneratedMetadataBase : ComponentBase
{
    [Parameter]
    public string BaseParameter { get; private set; } = "";

    [CascadingParameter(Name = "aot-cascade")]
    public string? NamedCascade { get; private set; }

    [Inject(Key = "aot-key")]
    private IGreetingService? KeyedGreeting { get; set; }

    protected string KeyedGreetingText => KeyedGreeting?.Greeting ?? "(none)";
}
