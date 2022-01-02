// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies the name of an action.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ActionNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new <see cref="ActionNameAttribute"/> instance.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    public ActionNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the action.
    /// </summary>
    public string Name { get; }
}
