// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.InternalTesting.Tracing;

namespace Microsoft.AspNetCore.Internal.Tests;

public class CertificateManagerEventSourceTests
{
    [Fact]
    public void EventIdsAreConsistent()
    {
        EventSourceValidator.ValidateEventSourceIds<CertificateManager.CertificateManagerEventSource>();
    }
}
