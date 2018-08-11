// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    public class DefaultPolicyProviderTests
    {
        [Fact]
        public async Task UsesTheDefaultPolicyName()
        {
            // Arrange
            var options = new CorsOptions();
            var policy = new CorsPolicy();
            options.AddPolicy(options.DefaultPolicyName, policy);

            var corsOptions = Options.Create(options);
            var policyProvider = new DefaultCorsPolicyProvider(corsOptions);

            // Act
            var actualPolicy = await policyProvider.GetPolicyAsync(new DefaultHttpContext(), policyName: null);

            // Assert
            Assert.Same(policy, actualPolicy);
        }

        [Theory]
        [InlineData("")]
        [InlineData("policyName")]
        public async Task GetsNamedPolicy(string policyName)
        {
            // Arrange
            var options = new CorsOptions();
            var policy = new CorsPolicy();
            options.AddPolicy(policyName, policy);

            var corsOptions = Options.Create(options);
            var policyProvider = new DefaultCorsPolicyProvider(corsOptions);

            // Act
            var actualPolicy = await policyProvider.GetPolicyAsync(new DefaultHttpContext(), policyName);

            // Assert
            Assert.Same(policy, actualPolicy);
        }
    }
}