// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class HttpLoggingServicesExtensionsTests
    {
        [Fact]
        public void AddHttpLogging_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceCollection().AddHttpLogging(null));
        }

        [Fact]
        public void AddW3CLogging_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceCollection().AddW3CLogging(null));
        }
    }
}
