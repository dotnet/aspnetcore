// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.DotNet.Watcher.Tools;

namespace Microsoft.DotNet.Watcher.Internal
{
    internal static class Ensure
    {
        public static T NotNull<T>(T obj, string paramName)
            where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(paramName);
            }
            return obj;
        }

        public static string NotNullOrEmpty(string obj, string paramName)
        {
            if (string.IsNullOrEmpty(obj))
            {
                throw new ArgumentException(Resources.Error_StringNullOrEmpty, paramName);
            }
            return obj;
        }
    }
}
