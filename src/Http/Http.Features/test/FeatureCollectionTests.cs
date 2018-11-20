// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Features
{
    public class FeatureCollectionTests
    {
        [Fact]
        public void AddedInterfaceIsReturned()
        {
            var interfaces = new FeatureCollection();
            var thing = new Thing();

            interfaces[typeof(IThing)] = thing;

            object thing2 = interfaces[typeof(IThing)];
            Assert.Equal(thing2, thing);
        }

        [Fact]
        public void IndexerAlsoAddsItems()
        {
            var interfaces = new FeatureCollection();
            var thing = new Thing();

            interfaces[typeof(IThing)] = thing;

            Assert.Equal(interfaces[typeof(IThing)], thing);
        }

        [Fact]
        public void SetNullValueRemoves()
        {
            var interfaces = new FeatureCollection();
            var thing = new Thing();

            interfaces[typeof(IThing)] = thing;
            Assert.Equal(interfaces[typeof(IThing)], thing);

            interfaces[typeof(IThing)] = null;

            object thing2 = interfaces[typeof(IThing)];
            Assert.Null(thing2);
        }
    }
}
