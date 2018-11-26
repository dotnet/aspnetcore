// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteValuesAddressMetadataTests
    {
        [Fact]
        public void DebuggerToString_NoNameAndRequiredValues_ReturnsString()
        {
            var metadata = new RouteValuesAddressMetadata(null, new Dictionary<string, object>());

            Assert.Equal("Name:  - Required values: ", metadata.DebuggerToString());
        }

        [Fact]
        public void DebuggerToString_HasNameAndRequiredValues_ReturnsString()
        {
            var metadata = new RouteValuesAddressMetadata("Name!", new Dictionary<string, object>
            {
                ["requiredValue1"] = "One",
                ["requiredValue2"] = 2,
            });

            Assert.Equal("Name: Name! - Required values: requiredValue1 = \"One\", requiredValue2 = \"2\"", metadata.DebuggerToString());
        }
    }
}
