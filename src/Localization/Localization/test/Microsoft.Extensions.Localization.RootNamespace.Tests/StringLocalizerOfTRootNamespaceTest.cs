// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LocalizationTest.Abc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Localization.RootNamespace.Tests
{
    public class StringLocalizerOfTRootNamespaceTest
    {
        [Fact]
        public void RootNamespace()
        {
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new ResourceManagerStringLocalizerFactory(options.Object, NullLoggerFactory.Instance);

            var valuesLoc = factory.Create(typeof(ValuesController));
            Assert.Equal("ValFromResource", valuesLoc["String1"]);
        }
    }
}
