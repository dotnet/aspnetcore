// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DiagnosticsViewPage.Views
{
    [Obsolete("This type is for internal use only and will be removed in a future version.")]
    public class AttributeValue
    {
        public AttributeValue(string prefix, object value, bool literal)
        {
            Prefix = prefix;
            Value = value;
            Literal = literal;
        }

        public string Prefix { get; }

        public object Value { get; }

        public bool Literal { get; }

        public static AttributeValue FromTuple(Tuple<string, object, bool> value)
        {
            return new AttributeValue(value.Item1, value.Item2, value.Item3);
        }

        public static AttributeValue FromTuple(Tuple<string, string, bool> value)
        {
            return new AttributeValue(value.Item1, value.Item2, value.Item3);
        }

        public static implicit operator AttributeValue(Tuple<string, object, bool> value)
        {
            return FromTuple(value);
        }
    }
}