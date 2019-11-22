// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.StackTrace.Sources;

namespace Microsoft.AspNetCore.Hosting.Views
{
    /// <summary>
    /// Holds data to be displayed on the error page.
    /// </summary>
    internal class ErrorPageModel
    {
        /// <summary>
        /// Detailed information about each exception in the stack.
        /// </summary>
        public IEnumerable<ExceptionDetails> ErrorDetails { get; set; }

        public bool ShowRuntimeDetails { get; set; }

        public string RuntimeDisplayName { get; set; }

        public string RuntimeArchitecture { get; set; }

        public string ClrVersion { get; set; }

        public string CurrentAssemblyVesion { get; set; }

        public string OperatingSystemDescription { get; set; }
    }
}
