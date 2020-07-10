// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Identifies meta elements by a shared attribute.
    /// </summary>
    internal readonly struct MetaElementKey
    {
        public string Name { get; }

        public string Value { get; }

        public MetaElementKey(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override bool Equals(object? obj)
            => obj is MetaElementKey other && string.Equals(Name, other.Name) && string.Equals(Value, other.Value); 

        public override int GetHashCode()
            => Name.GetHashCode() ^ Value.GetHashCode();
    }
}
