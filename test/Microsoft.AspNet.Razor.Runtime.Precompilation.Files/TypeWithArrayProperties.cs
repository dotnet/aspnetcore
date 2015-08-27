// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    public class TypeWithArrayProperties
    {
        public MyAbstractClass[] AbstractArray { get; set; }

        public IDisposable[] DisposableArray { get; }

        public ContainerType.NestedType[] NestedArrayType { get; }

        internal InternalType[] InternalArray { get; set; }

        public ICollection<IDictionary<string, IList<object[]>>>[] GenericArray { get; set; }
    }
}
