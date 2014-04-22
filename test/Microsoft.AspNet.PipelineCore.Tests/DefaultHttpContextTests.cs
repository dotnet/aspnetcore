using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Xunit;

namespace Microsoft.AspNet.PipelineCore.Tests
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
    }
}