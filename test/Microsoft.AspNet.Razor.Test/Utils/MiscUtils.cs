// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.TestCommon;

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
