// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public class AuthenticateContextTest
    {
        [Fact]
        public void AuthenticateContext_Authenticated()
        {
            // Arrange
            var context = new AuthenticateContext("test");

            var principal = new ClaimsPrincipal();
            var properties = new Dictionary<string, string>();
            var description = new Dictionary<string, object>();

            // Act
            context.Authenticated(principal, properties, description);

            // Assert
            Assert.True(context.Accepted);
            Assert.Equal("test", context.AuthenticationScheme);
            Assert.Same(description, context.Description);
            Assert.Null(context.Error);
            Assert.Same(principal, context.Principal);
            Assert.Same(properties, context.Properties);
        }

        [Fact]
        public void AuthenticateContext_Authenticated_SetsUnusedPropertiesToDefault()
        {
            // Arrange
            var context = new AuthenticateContext("test");

            var principal = new ClaimsPrincipal();
            var properties = new Dictionary<string, string>();
            var description = new Dictionary<string, object>();

            context.Failed(new Exception());

            // Act
            context.Authenticated(principal, properties, description);

            // Assert
            Assert.True(context.Accepted);
            Assert.Equal("test", context.AuthenticationScheme);
            Assert.Same(description, context.Description);
            Assert.Null(context.Error);
            Assert.Same(principal, context.Principal);
            Assert.Same(properties, context.Properties);
        }

        [Fact]
        public void AuthenticateContext_Failed()
        {
            // Arrange
            var context = new AuthenticateContext("test");

            var exception = new Exception();

            // Act
            context.Failed(exception);

            // Assert
            Assert.True(context.Accepted);
            Assert.Equal("test", context.AuthenticationScheme);
            Assert.Null(context.Description);
            Assert.Same(exception, context.Error);
            Assert.Null(context.Principal);
            Assert.Null(context.Properties);
        }

        [Fact]
        public void AuthenticateContext_Failed_SetsUnusedPropertiesToDefault()
        {
            // Arrange
            var context = new AuthenticateContext("test");

            var exception = new Exception();

            context.Authenticated(new ClaimsPrincipal(), new Dictionary<string, string>(), new Dictionary<string, object>());

            // Act
            context.Failed(exception);

            // Assert
            Assert.True(context.Accepted);
            Assert.Equal("test", context.AuthenticationScheme);
            Assert.Null(context.Description);
            Assert.Same(exception, context.Error);
            Assert.Null(context.Principal);
            Assert.Null(context.Properties);
        }

        [Fact]
        public void AuthenticateContext_NotAuthenticated()
        {
            // Arrange
            var context = new AuthenticateContext("test");

            // Act
            context.NotAuthenticated();

            // Assert
            Assert.True(context.Accepted);
            Assert.Equal("test", context.AuthenticationScheme);
            Assert.Null(context.Description);
            Assert.Null(context.Error);
            Assert.Null(context.Principal);
            Assert.Null(context.Properties);
        }

        [Fact]
        public void AuthenticateContext_NotAuthenticated_SetsUnusedPropertiesToDefault_Authenticated()
        {
            // Arrange
            var context = new AuthenticateContext("test");

            var exception = new Exception();

            context.Authenticated(new ClaimsPrincipal(), new Dictionary<string, string>(), new Dictionary<string, object>());

            // Act
            context.NotAuthenticated();

            // Assert
            Assert.True(context.Accepted);
            Assert.Equal("test", context.AuthenticationScheme);
            Assert.Null(context.Description);
            Assert.Null(context.Error);
            Assert.Null(context.Principal);
            Assert.Null(context.Properties);
        }

        [Fact]
        public void AuthenticateContext_NotAuthenticated_SetsUnusedPropertiesToDefault_Failed()
        {
            // Arrange
            var context = new AuthenticateContext("test");
            
            context.Failed(new Exception());

            context.NotAuthenticated();

            // Assert
            Assert.True(context.Accepted);
            Assert.Equal("test", context.AuthenticationScheme);
            Assert.Null(context.Description);
            Assert.Null(context.Error);
            Assert.Null(context.Principal);
            Assert.Null(context.Properties);
        }
    }
}
