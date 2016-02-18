// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.Elm
{
    /// <summary>
    /// Options for ElmMiddleware
    /// </summary>
    public class ElmOptions
    {
        /// <summary>
        /// Specifies the path to view the logs.
        /// </summary>
        public PathString Path { get; set; } = new PathString("/Elm");

        /// <summary>
        /// Determines whether log statements should be logged based on the name of the logger
        /// and the <see cref="M:LogLevel"/> of the message.
        /// </summary>
        public Func<string, LogLevel, bool> Filter { get; set; } = (name, level) => true;
    }
}