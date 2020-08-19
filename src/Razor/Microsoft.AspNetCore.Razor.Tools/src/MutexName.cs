// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal static class MutexName
    {
        public static string GetClientMutexName(string pipeName)
        {
            return $"{pipeName}.client";
        }

        public static string GetServerMutexName(string pipeName)
        {
            // We want to prefix this with Global\ because we want this mutex to be visible
            // across terminal sessions which is useful for cases like shutdown.
            // https://msdn.microsoft.com/en-us/library/system.threading.mutex(v=vs.110).aspx#Remarks
            // This still wouldn't allow other users to access the server because the pipe will fail to connect.
            return $"Global\\{pipeName}.server";
        }
    }
}
