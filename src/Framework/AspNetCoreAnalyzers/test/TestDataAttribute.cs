// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal class TestDataAttribute : Attribute
{
    public TestDataAttribute(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }
    public string Value { get; }
}
