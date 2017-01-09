// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.StaticFiles
{
    public static class TestDirectory
    {
        public static readonly string BaseDirectory
#if NET451
        = AppDomain.CurrentDomain.BaseDirectory;
#else
        = AppContext.BaseDirectory;
#endif
    }
}