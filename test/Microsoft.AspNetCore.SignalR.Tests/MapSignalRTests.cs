using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class MapSignalRTests
    {
        [Fact]
        public void MapSignalRFailsForInvalidHub()
        {
            var ex = Assert.Throws<NotSupportedException>(() =>
            {
                var builder = new WebHostBuilder()
                        .UseKestrel()
                        .ConfigureServices(services =>
                        {
                            services.AddSignalR();
                        })
                        .Configure(app =>
                        {
                            app.UseSignalR(options => options.MapHub<InvalidHub>("overloads"));
                        })
                        .Build();
            });

            Assert.Equal("Duplicate definitions of 'OverloadedMethod'. Overloading is not supported.", ex.Message);
        }

        private class InvalidHub : Hub
        {
            public void OverloadedMethod(int num)
            {
            }

            public void OverloadedMethod(string message)
            {
            }
        }
    }
}
