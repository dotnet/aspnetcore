// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace DispatcherSample
{
    public interface ICorsPolicyMetadata
    {
        string Name { get; }
    }

    public class CorsPolicyMetadata : ICorsPolicyMetadata
    {
        public CorsPolicyMetadata(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
