// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Features.Internal;
using Xunit;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultHttpContextTests
    {
        [Fact]
        public void EmptyUserIsNeverNull()
        {
            var context = new DefaultHttpContext(new FeatureCollection());
            Assert.NotNull(context.User);
            Assert.Equal(1, context.User.Identities.Count());
            Assert.True(object.ReferenceEquals(context.User, context.User));
            Assert.False(context.User.Identity.IsAuthenticated);
            Assert.True(string.IsNullOrEmpty(context.User.Identity.AuthenticationType));

            context.User = null;
            Assert.NotNull(context.User);
            Assert.Equal(1, context.User.Identities.Count());
            Assert.True(object.ReferenceEquals(context.User, context.User));
            Assert.False(context.User.Identity.IsAuthenticated);
            Assert.True(string.IsNullOrEmpty(context.User.Identity.AuthenticationType));

            context.User = new ClaimsPrincipal();
            Assert.NotNull(context.User);
            Assert.Equal(0, context.User.Identities.Count());
            Assert.True(object.ReferenceEquals(context.User, context.User));
            Assert.Null(context.User.Identity);

            context.User = new ClaimsPrincipal(new ClaimsIdentity("SomeAuthType"));
            Assert.Equal("SomeAuthType", context.User.Identity.AuthenticationType);
            Assert.True(context.User.Identity.IsAuthenticated);
        }

        [Fact]
        public void GetItems_DefaultCollectionProvided()
        {
            var context = new DefaultHttpContext(new FeatureCollection());
            Assert.Null(context.GetFeature<IItemsFeature>());
            var items = context.Items;
            Assert.NotNull(context.GetFeature<IItemsFeature>());
            Assert.NotNull(items);
            Assert.Same(items, context.Items);
            var item = new object();
            context.Items["foo"] = item;
            Assert.Same(item, context.Items["foo"]);
        }

        [Fact]
        public void SetItems_NewCollectionUsed()
        {
            var context = new DefaultHttpContext(new FeatureCollection());
            Assert.Null(context.GetFeature<IItemsFeature>());
            var items = new Dictionary<object, object>();
            context.Items = items;
            Assert.NotNull(context.GetFeature<IItemsFeature>());
            Assert.Same(items, context.Items);
            var item = new object();
            items["foo"] = item;
            Assert.Same(item, context.Items["foo"]);
        }

        private HttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            return context;
        }
    }
}