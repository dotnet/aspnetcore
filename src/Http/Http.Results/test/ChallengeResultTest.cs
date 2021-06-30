// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class ChallengeResultTest
    {
        [Fact]
        public async Task ChallengeResult_ExecuteAsync()
        {
            // Arrange
            var result = new ChallengeResult("", null);
            var auth = new Mock<IAuthenticationService>();
            var httpContext = GetHttpContext(auth);

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            auth.Verify(c => c.ChallengeAsync(httpContext, "", null), Times.Exactly(1));
        }

        [Fact]
        public async Task ChallengeResult_ExecuteAsync_NoSchemes()
        {
            // Arrange
            var result = new ChallengeResult(new string[] { }, null);
            var auth = new Mock<IAuthenticationService>();
            var httpContext = GetHttpContext(auth);

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            auth.Verify(c => c.ChallengeAsync(httpContext, null, null), Times.Exactly(1));
        }
        
        private static DefaultHttpContext GetHttpContext(Mock<IAuthenticationService> auth)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices()
                .AddSingleton(auth.Object)
                .BuildServiceProvider();
            return httpContext;
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            return services;
        }
    }
}
