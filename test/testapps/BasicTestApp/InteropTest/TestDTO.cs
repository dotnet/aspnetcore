// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace BasicTestApp.InteropTest
{
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
}
