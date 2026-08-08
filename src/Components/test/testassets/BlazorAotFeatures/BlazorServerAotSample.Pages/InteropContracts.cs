// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace BlazorServerAotSample.Pages;

public sealed record InteropRequest(string Name, int Age);

public sealed record InteropResult(string Name, int Age);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Cat), "cat")]
public abstract class Animal
{
    public string Name { get; set; } = "";
}

public sealed class Cat : Animal
{
    public int Lives { get; set; }
}
