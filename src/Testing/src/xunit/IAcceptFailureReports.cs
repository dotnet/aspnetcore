// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Testing;

internal interface IAcceptFailureReports
{
    void ReportTestFailure();
}
