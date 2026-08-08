// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorServerAotSample.Pages;

/// <summary>
/// Trivial service used by the PoC to exercise the source-generated <c>[Inject]</c> binder (L3).
/// </summary>
public interface IGreetingService
{
    string Greeting { get; }
}

/// <summary>
/// Default <see cref="IGreetingService"/> implementation registered by the host.
/// </summary>
public sealed class GreetingService : IGreetingService
{
    public string Greeting => "injected-greeting";
}
