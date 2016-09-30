// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !NETSTANDARD1_3 // TODO: Temp copy. Remove once we target net46.
using System;
namespace System.Threading
{
    internal interface IDeferredDisposable
    {
        void OnFinalRelease(bool disposed);
    }
}
#endif