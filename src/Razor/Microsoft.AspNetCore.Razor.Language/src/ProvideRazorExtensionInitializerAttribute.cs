// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class ProvideRazorExtensionInitializerAttribute : Attribute
{
    public ProvideRazorExtensionInitializerAttribute(string extensionName, Type initializerType)
    {
        if (extensionName == null)
        {
            throw new ArgumentNullException(nameof(extensionName));
        }

        if (initializerType == null)
        {
            throw new ArgumentNullException(nameof(initializerType));
        }

        ExtensionName = extensionName;
        InitializerType = initializerType;
    }

    public string ExtensionName { get; }

    public Type InitializerType { get; }
}
