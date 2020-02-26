// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Caching.SqlServer
{
    internal static class PlatformHelper
    {
        private static Lazy<bool> _isMono = new Lazy<bool>(() => Type.GetType("Mono.Runtime") != null);

        public static bool IsMono
        {
            get
            {
                return _isMono.Value;
            }
        }
    }
}