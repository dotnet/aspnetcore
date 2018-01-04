// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.TagHelperTool
{
    internal static class MutexName
    {
        public static string GetClientMutexName(string pipeName)
        {
            return $"{pipeName}.client";
        }

        public static string GetServerMutexName(string pipeName)
        {
            return $"{pipeName}.server";
        }
    }
}
