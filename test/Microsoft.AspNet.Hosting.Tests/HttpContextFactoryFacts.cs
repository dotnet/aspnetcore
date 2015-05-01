// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Owin;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class HttpContextFactoryFacts
    {
        [Fact]
        public void Mutable_FeatureCollection_Wrapped_For_OwinFeatureCollection()
        {
            var env = new Dictionary<string, object>();
            var contextFactory = new HttpContextFactory();
            var context = contextFactory.CreateHttpContext(new OwinFeatureCollection(env));
            
            // Setting a feature will throw if the above feature collection is not wrapped in a mutable feature collection.
            context.SetFeature<ICustomFeature>(new CustomFeature(100));
            Assert.Equal(100, context.GetFeature<ICustomFeature>().Value);
        }

        private interface ICustomFeature
        {
            int Value { get; }
        }

        private class CustomFeature : ICustomFeature
        {
            private readonly int _value;
            public CustomFeature(int value)
            {
                _value = value;
            }

            public int Value
            {
                get { return _value; }
            }
        }
    }
}