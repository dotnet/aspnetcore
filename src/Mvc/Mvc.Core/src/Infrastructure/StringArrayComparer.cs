// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class StringArrayComparer : IEqualityComparer<string[]>
    {
        public static readonly StringArrayComparer Ordinal = new StringArrayComparer(StringComparer.Ordinal);

        public static readonly StringArrayComparer OrdinalIgnoreCase = new StringArrayComparer(StringComparer.OrdinalIgnoreCase);

        private readonly StringComparer _valueComparer;

        private StringArrayComparer(StringComparer valueComparer)
        {
            _valueComparer = valueComparer;
        }

        public bool Equals(string[] x, string[] y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null ^ y == null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (string.IsNullOrEmpty(x[i]) && string.IsNullOrEmpty(y[i]))
                {
                    continue;
                }

                if (!_valueComparer.Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(string[] obj)
        {
            if (obj == null)
            {
                return 0;
            }

            var hash = new HashCodeCombiner();
            for (var i = 0; i < obj.Length; i++)
            {
                var o = obj[i];

                // Route values define null and "" to be equivalent.
                if (string.IsNullOrEmpty(o))
                {
                    o = null;
                }
                hash.Add(o, _valueComparer);
            }

            return hash.CombinedHash;
        }
    }
}
