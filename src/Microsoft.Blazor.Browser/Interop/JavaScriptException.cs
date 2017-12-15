// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Blazor.Browser.Interop
{
    public class JavaScriptException : Exception
    {
        internal JavaScriptException(string message) : base(message)
        {
        }
    }
}
