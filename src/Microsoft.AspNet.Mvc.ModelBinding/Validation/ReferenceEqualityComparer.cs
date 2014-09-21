// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    internal class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        private static readonly ReferenceEqualityComparer _instance = new ReferenceEqualityComparer();

        public static ReferenceEqualityComparer Instance
        {
            get
            {
                return _instance;
            }
        }

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}