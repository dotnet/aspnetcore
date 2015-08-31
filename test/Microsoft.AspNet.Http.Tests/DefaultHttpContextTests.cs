// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Xunit;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultHttpContextTests
    {
        [Fact]
        public void GetOnSessionProperty_ThrowsOnMissingSessionFeature()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => context.Session);
            Assert.Equal("Session has not been configured for this application or request.", exception.Message);
        }

        [Fact]
        public void GetOnSessionProperty_ReturnsAvailableSession()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var session = new TestSession();
            session.Set("key1", null);
            session.Set("key2", null);
            var feature = new BlahSessionFeature();
            feature.Session = session;
            context.Features.Set<ISessionFeature>(feature);

            // Act & Assert
            Assert.Same(session, context.Session);
            context.Session.Set("key3", null);
            Assert.Equal(3, context.Session.Keys.Count());
        }

        [Fact]
        public void AllowsSettingSession_WithoutSettingUpSessionFeature_Upfront()
        {
            // Arrange
            var session = new TestSession();
            var context = new DefaultHttpContext();

            // Act
            context.Session = session;

            // Assert
            Assert.Same(session, context.Session);
        }

        [Fact]
        public void SettingSession_OverridesAvailableSession()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var session = new TestSession();
            session.Set("key1", null);
            session.Set("key2", null);
            var feature = new BlahSessionFeature();
            feature.Session = session;
            context.Features.Set<ISessionFeature>(feature);

            // Act
            context.Session = new TestSession();

            // Assert
            Assert.NotSame(session, context.Session);
            Assert.Empty(context.Session.Keys);
        }

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
            Assert.Null(context.Features.Get<IItemsFeature>());
            var items = context.Items;
            Assert.NotNull(context.Features.Get<IItemsFeature>());
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
            Assert.Null(context.Features.Get<IItemsFeature>());
            var items = new Dictionary<object, object>();
            context.Items = items;
            Assert.NotNull(context.Features.Get<IItemsFeature>());
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

        private class TestSession : ISession
        {
            private Dictionary<string, byte[]> _store
                = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            public IEnumerable<string> Keys { get { return _store.Keys; } }

            public void Clear()
            {
                _store.Clear();
            }

            public Task CommitAsync()
            {
                return Task.FromResult(0);
            }

            public Task LoadAsync()
            {
                return Task.FromResult(0);
            }

            public void Remove(string key)
            {
                _store.Remove(key);
            }

            public void Set(string key, byte[] value)
            {
                _store[key] = value;
            }

            public bool TryGetValue(string key, out byte[] value)
            {
                return _store.TryGetValue(key, out value);
            }
        }

        private class BlahSessionFeature : ISessionFeature
        {
            public ISession Session { get; set; }
        }
    }
}