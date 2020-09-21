// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Diagnostics
{
    public class ExceptionHandlerFeature : IExceptionHandlerPathFeature
    {
        public Exception Error { get; set; } = default!;

        public string Path { get; set; } = default!;
    }
}
