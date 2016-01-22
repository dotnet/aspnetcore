// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNet.Session
{
    public static class TestExtensions
    {
        public static IEnumerable<WriteContext> OnlyMessagesFromSource<T>(this IEnumerable<WriteContext> source)
        {
            return source.Where(message => message.LoggerName.Equals(typeof(T).FullName, StringComparison.Ordinal));
        }
    }
}
