// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.UI;

/// <summary>
/// The UIFramework Identity UI will use on the application.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class UIFrameworkAttribute : Attribute
{

    /// <summary>
    /// Initializes a new instance of <see cref="UIFrameworkAttribute"/>.
    /// </summary>
    /// <param name="uiFramework"></param>
    public UIFrameworkAttribute(string uiFramework)
    {
        UIFramework = uiFramework;
    }

    /// <summary>
    /// The UI Framework Identity UI will use.
    /// </summary>
    public string UIFramework { get; }
}
