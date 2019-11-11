// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class ItemsDictionaryTests
    {
        [Fact]
        public void GetEnumerator_ShouldResolveWithoutNullReferenceException()
        {
            // Arrange
            var dict = new ItemsDictionary();

            // Act and Assert
            IEnumerable en = (IEnumerable) dict;
            Assert.NotNull(en.GetEnumerator());
        }
    }
}
