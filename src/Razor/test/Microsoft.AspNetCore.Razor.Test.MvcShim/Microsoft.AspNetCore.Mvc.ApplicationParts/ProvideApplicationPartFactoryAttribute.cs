// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ProvideApplicationPartFactoryAttribute : Attribute
{
    public ProvideApplicationPartFactoryAttribute(Type factoryType)
    {
    }

    public ProvideApplicationPartFactoryAttribute(string factoryTypeName)
    {
    }
}
