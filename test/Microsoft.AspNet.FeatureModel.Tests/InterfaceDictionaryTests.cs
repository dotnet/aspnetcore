// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.FeatureModel.Tests
{
    public class InterfaceDictionaryTests
    {
        [Fact]
        public void AddedInterfaceIsReturned()
        {
            var interfaces = new FeatureCollection();
            var thing = new Thing();

            interfaces.Add(typeof(IThing), thing);

            Assert.Equal(interfaces[typeof(IThing)], thing);

            object thing2;
            Assert.True(interfaces.TryGetValue(typeof(IThing), out thing2));
            Assert.Equal(thing2, thing);
        }

        [Fact]
        public void IndexerAlsoAddsItems()
        {
            var interfaces = new FeatureCollection();
            var thing = new Thing();

            interfaces[typeof(IThing)] = thing;

            Assert.Equal(interfaces[typeof(IThing)], thing);

            object thing2;
            Assert.True(interfaces.TryGetValue(typeof(IThing), out thing2));
            Assert.Equal(thing2, thing);
        }

        [Fact]
        public void SecondCallToAddThrowsException()
        {
            var interfaces = new FeatureCollection();
            var thing = new Thing();

            interfaces.Add(typeof(IThing), thing);

            Assert.Throws<ArgumentException>(() => interfaces.Add(typeof(IThing), thing));
        }
    }
}
