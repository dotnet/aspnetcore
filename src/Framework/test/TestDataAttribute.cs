// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore
{
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
}
