// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace BlazorServerAotSample.Pages;

public class JsInvokableBase
{
    [JSInvokable("BaseEcho")]
    public string BaseEcho(int value) => $"base:{value}";
}

public sealed class JsInvokableDerived : JsInvokableBase
{
    [JSInvokable("StaticEcho")]
    public static string StaticEcho(int value) => $"static:{value}";

    [JSInvokable]
    public string InstanceEcho(int value) => $"instance:{value}";

    [JSInvokable]
    public void VoidCall() { }

    [JSInvokable]
    public string SyncCall() => "sync";

    [JSInvokable]
    public Task TaskCall() => Task.CompletedTask;

    [JSInvokable]
    public Task<string> TaskOfTCall() => Task.FromResult("task");

    [JSInvokable]
    public ValueTask ValueTaskCall() => ValueTask.CompletedTask;

    [JSInvokable]
    public ValueTask<string> ValueTaskOfTCall() => ValueTask.FromResult("value-task");

    [JSInvokable]
    public InteropResult EchoPoco(InteropRequest request) =>
        new(request.Name.ToUpperInvariant(), request.Age + 1);

    [JSInvokable]
    public Animal EchoAnimal(Animal animal) => animal;
}
