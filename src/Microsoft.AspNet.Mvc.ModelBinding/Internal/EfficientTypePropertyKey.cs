// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    internal class EfficientTypePropertyKey<T1, T2> : Tuple<T1, T2>
    {
        private int _hashCode;

        public EfficientTypePropertyKey(T1 item1, T2 item2)
            : base(item1, item2)
        {
            _hashCode = base.GetHashCode();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
