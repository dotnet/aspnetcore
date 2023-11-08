// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class TestFrameworkFileLoggerAttribute : TestOutputDirectoryAttribute
{
    public TestFrameworkFileLoggerAttribute(string preserveExistingLogsInOutput, string tfm, string baseDirectory = null)
        : base(preserveExistingLogsInOutput, tfm, baseDirectory)
    {
    }
}
