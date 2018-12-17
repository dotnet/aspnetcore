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
#pragma warning disable CS0618 // Type or member is obsolete
            var metadata = new RouteValuesAddressMetadata(null, new Dictionary<string, object>());
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal("Name:  - Required values: ", metadata.DebuggerToString());
        }

        [Fact]
        public void DebuggerToString_HasNameAndRequiredValues_ReturnsString()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var metadata = new RouteValuesAddressMetadata("Name!", new Dictionary<string, object>
            {
                ["requiredValue1"] = "One",
                ["requiredValue2"] = 2,
            });
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal("Name: Name! - Required values: requiredValue1 = \"One\", requiredValue2 = \"2\"", metadata.DebuggerToString());
        }
    }
}
