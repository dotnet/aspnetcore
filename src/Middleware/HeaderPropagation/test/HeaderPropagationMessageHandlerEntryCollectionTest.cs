// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationMessageHandlerEntryCollectionTest
    {
        [Fact]
        public void Add_SingleValue_UseValueForBothProperties()
        {
            var collection = new HeaderPropagationMessageHandlerEntryCollection();
            collection.Add("foo");

            Assert.Single(collection);
            var entry = collection[0];
            Assert.Equal("foo", entry.CapturedHeaderName);
            Assert.Equal("foo", entry.OutboundHeaderName);
        }

        [Fact]
        public void Add_BothValues_UseCorrectValues()
        {
            var collection = new HeaderPropagationMessageHandlerEntryCollection();
            collection.Add("foo", "bar");

            Assert.Single(collection);
            var entry = collection[0];
            Assert.Equal("foo", entry.CapturedHeaderName);
            Assert.Equal("bar", entry.OutboundHeaderName);
        }
    }
}
