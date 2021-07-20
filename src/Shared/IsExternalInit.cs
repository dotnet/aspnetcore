// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // this class is needed for init properties.
    // It was added to .NET 5.0 but for earlier versions we need to specify it manually
#if !NET5_0_OR_GREATER
    internal static class IsExternalInit { }
#endif
}
