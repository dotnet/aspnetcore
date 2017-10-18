// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    public class RoutePatternException : Exception
    {
        public RoutePatternException(string pattern, string message)
            : base(message)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Pattern = pattern;
        }

        public string Pattern { get; }
    }
}
