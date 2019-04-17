// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    // End-to-end tests for the host matching functionality
    public class HostMatcherPolicyIEndpointSelectorPolicyIntegrationTest : HostMatcherPolicyIntegrationTestBase
    {
        protected override bool HasDynamicMetadata => true;
    }
}
