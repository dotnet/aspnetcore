// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public static class ErrorUtilities
    {
        public static void ThrowInvalidRequestLine()
        {
            throw new InvalidOperationException("Invalid request line");
        }

        public static void ThrowInvalidRequestHeaders()
        {
            throw new InvalidOperationException("Invalid request headers");
        }
    }
}
