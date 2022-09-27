// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Customizes the name of a hub method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class HubMethodNameAttribute : Attribute
{
    /// <summary>
    /// The customized name of the hub method.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HubMethodNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The customized name of the hub method.</param>
    public HubMethodNameAttribute(string name)
    {
        Name = name;
    }
}
