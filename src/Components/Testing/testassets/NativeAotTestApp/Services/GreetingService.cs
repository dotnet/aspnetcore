// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NativeAotTestApp.Services;

public interface IGreetingService
{
    string Greeting { get; }
}

public sealed class GreetingService : IGreetingService
{
    public string Greeting => "injected-greeting";
}
