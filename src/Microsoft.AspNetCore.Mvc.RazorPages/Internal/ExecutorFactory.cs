// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class ExecutorFactory
    {
        public static Func<Page, object, Task<IActionResult>> Create(MethodInfo method)
        {
            throw new NotImplementedException();
        }
    }
}