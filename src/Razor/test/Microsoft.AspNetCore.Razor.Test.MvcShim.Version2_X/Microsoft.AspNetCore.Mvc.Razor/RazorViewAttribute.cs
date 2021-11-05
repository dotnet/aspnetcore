// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RazorViewAttribute : Attribute
{
    public RazorViewAttribute(string path, Type viewType)
    {
        Path = path;
        ViewType = viewType;
    }

    /// <summary>
    /// Gets the path of the view.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the view type.
    /// </summary>
    public Type ViewType { get; }
}
