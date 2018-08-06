// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    public class AddWebSocketsTests
    {
        [Fact]
        public void AddWebSocketsConfiguresOptions()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddWebSockets(o =>
            {
                o.KeepAliveInterval = TimeSpan.FromSeconds(1000);
                o.AllowedOrigins.Add("someString");
            });

            var services = serviceCollection.BuildServiceProvider();
            var socketOptions = services.GetRequiredService<IOptions<WebSocketOptions>>().Value;

            Assert.Equal(TimeSpan.FromSeconds(1000), socketOptions.KeepAliveInterval);
            Assert.Single(socketOptions.AllowedOrigins);
            Assert.Equal("someString", socketOptions.AllowedOrigins[0]);
        }
    }
}
