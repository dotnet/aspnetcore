// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Testing
{
    /// <summary>
    /// Capture the memory dump upon test failure.
    /// </summary>
    /// <remarks>
    /// This currently only works in Windows environments
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CollectDumpAttribute : Attribute
    {
    }
}
