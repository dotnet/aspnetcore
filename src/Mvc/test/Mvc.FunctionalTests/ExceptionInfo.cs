// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// Information about an exception that occurred on the server side of a functional
/// test.
/// </summary>
public class ExceptionInfo
{
    public string ExceptionMessage { get; set; }

    public string ExceptionType { get; set; }
}
