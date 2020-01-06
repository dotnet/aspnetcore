// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.Extensions.Logging.Testing
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class TestFrameworkFileLoggerAttribute : TestOutputDirectoryAttribute
    {
        public TestFrameworkFileLoggerAttribute(string preserveExistingLogsInOutput, string tfm, string baseDirectory = null)
            : base(preserveExistingLogsInOutput, tfm, baseDirectory)
        {
        }
    }
}
