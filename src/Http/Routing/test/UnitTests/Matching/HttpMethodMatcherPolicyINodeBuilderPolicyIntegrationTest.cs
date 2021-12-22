// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

// End-to-end tests for the HTTP method matching functionality
public class HttpMethodMatcherPolicyINodeBuilderPolicyIntegrationTestBase : HttpMethodMatcherPolicyIntegrationTestBase
{
    protected override bool HasDynamicMetadata => false;
}
