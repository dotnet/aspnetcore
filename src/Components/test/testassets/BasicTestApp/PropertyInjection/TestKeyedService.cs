// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp.PropertyInjection;

public sealed class TestKeyedService
{
    public object Value { get; private init; }

    public static TestKeyedService Create(object value)
    {
        return new()
        {
            Value = value,
        };
    }
}
