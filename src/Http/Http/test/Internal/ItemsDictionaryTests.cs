// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            var dict = new Microsoft.AspNetCore.Http.ItemsDictionary();

            // Act and Assert
            System.Collections.IEnumerable en = (System.Collections.IEnumerable) dict;
            foreach(var item in en) // <-- in the original code, the implicit call to .GetEnumerator() would throw a NullReferenceException
            {
                // if there is a problem this code won't be reached
            }

            Assert.True(true, "The code should make it here without throwing an exception.");
        }
    }
}
