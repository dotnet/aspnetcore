// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if DEBUG
using System.Diagnostics;
#endif
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Utils
{
    class MiscUtils
    {
        public const int TimeoutInSeconds = 1;

        public static void DoWithTimeoutIfNotDebugging(Func<int, bool> withTimeout)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                withTimeout(Timeout.Infinite);
            }
            else
            {
#endif
                Assert.True(withTimeout((int)TimeSpan.FromSeconds(TimeoutInSeconds).TotalMilliseconds), "Timeout expired!");
#if DEBUG
            }
#endif
        }
    }
}
