// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorServerAotSample.Pages;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AotEndpointMarkerAttribute(string tag) : Attribute
{
    public string Tag { get; } = tag;
}
