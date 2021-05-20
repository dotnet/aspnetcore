// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Runtime.CompilerServices
{
    // this class is needed for init properties.
    // It was added to .NET 5.0 but for earlier versions we need to specify it manually
#if !NET5_0_OR_GREATER
    internal static class IsExternalInit { }
#endif
}
