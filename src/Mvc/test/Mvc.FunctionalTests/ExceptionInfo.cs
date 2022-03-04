// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    /// <summary>
    /// Information about an exception that occurred on the server side of a functional
    /// test.
    /// </summary>
    public class ExceptionInfo
    {
        public string ExceptionMessage { get; set; }

        public string ExceptionType { get; set; }
    }
}