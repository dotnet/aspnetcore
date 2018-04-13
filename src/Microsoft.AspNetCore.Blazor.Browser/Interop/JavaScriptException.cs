// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    /// <summary>
    /// Represents errors that occur during an interop call from .NET to JavaScript.
    /// </summary>
    public class JavaScriptException : Exception
    {
        internal JavaScriptException(string message) : base(message)
        {
        }
    }
}
