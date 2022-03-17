// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp.InteropTest;

public class TestDTO
{
    // JSON serialization won't include this in its output, nor will the JSON
    // deserializer be able to populate it. So if the value is retained, this
    // shows we're passing the object by reference, not via JSON marshalling.
    private readonly int _nonSerializedValue;

    public TestDTO(int nonSerializedValue)
    {
        _nonSerializedValue = nonSerializedValue;
    }

    public int GetNonSerializedValue()
        => _nonSerializedValue;
}
