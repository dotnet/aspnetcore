// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
