// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class TestHub : Hub
    {
        public string HelloWorld() => TestHubMethodsImpl.HelloWorld();

        public string Echo(string message) => TestHubMethodsImpl.Echo(message);

        public ChannelReader<int> Stream(int count) => TestHubMethodsImpl.Stream(count);

        public ChannelReader<int> StreamException() => TestHubMethodsImpl.StreamException();

        public ChannelReader<string> StreamBroken() => TestHubMethodsImpl.StreamBroken();

        public async Task CallEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Echo", message);
        }

        public async Task CallHandlerThatDoesntExist()
        {
            await Clients.Client(Context.ConnectionId).SendAsync("NoClientHandler");
        }

        public ChannelReader<string> StreamEcho(ChannelReader<string> source) => TestHubMethodsImpl.StreamEcho(source);

        public string GetUserIdentifier()
        {
            return Context.UserIdentifier;
        }

        public IEnumerable<string> GetHeaderValues(string[] headerNames)
        {
            var context = Context.GetHttpContext();

            if (context == null)
            {
                throw new InvalidOperationException("Unable to get HttpContext from request.");
            }

            var headers = context.Request.Headers;

            if (headers == null)
            {
                throw new InvalidOperationException("Unable to get headers from context.");
            }

            return headerNames.Select(h => (string)headers[h]);
        }

        public string GetCookieValue(string cookieName)
        {
            return Context.GetHttpContext().Request.Cookies[cookieName];
        }

        public object[] GetIHttpConnectionFeatureProperties()
        {
            var feature = Context.Features.Get<IHttpConnectionFeature>();

            object[] result =
            {
                feature.LocalPort,
                feature.RemotePort,
                feature.LocalIpAddress.ToString(),
                feature.RemoteIpAddress.ToString()
            };

            return result;
        }

        public string GetActiveTransportName()
        {
            return Context.Features.Get<IHttpTransportFeature>().TransportType.ToString();
        }
    }

    public class DynamicTestHub : DynamicHub
    {
        public string HelloWorld() => TestHubMethodsImpl.HelloWorld();

        public string Echo(string message) => TestHubMethodsImpl.Echo(message);

        public ChannelReader<int> Stream(int count) => TestHubMethodsImpl.Stream(count);

        public ChannelReader<int> StreamException() => TestHubMethodsImpl.StreamException();

        public ChannelReader<string> StreamBroken() => TestHubMethodsImpl.StreamBroken();

        public async Task CallEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).Echo(message);
        }

        public async Task CallHandlerThatDoesntExist()
        {
            await Clients.Client(Context.ConnectionId).NoClientHandler();
        }

        public ChannelReader<string> StreamEcho(ChannelReader<string> source) => TestHubMethodsImpl.StreamEcho(source);
    }

    public class TestHubT : Hub<ITestHub>
    {
        public string HelloWorld() => TestHubMethodsImpl.HelloWorld();

        public string Echo(string message) => TestHubMethodsImpl.Echo(message);

        public ChannelReader<int> Stream(int count) => TestHubMethodsImpl.Stream(count);

        public ChannelReader<int> StreamException() => TestHubMethodsImpl.StreamException();

        public ChannelReader<string> StreamBroken() => TestHubMethodsImpl.StreamBroken();

        public async Task CallEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).Echo(message);
        }

        public async Task CallHandlerThatDoesntExist()
        {
            await Clients.Client(Context.ConnectionId).NoClientHandler();
        }

        public ChannelReader<string> StreamEcho(ChannelReader<string> source) => TestHubMethodsImpl.StreamEcho(source);
    }

    internal static class TestHubMethodsImpl
    {
        public static string HelloWorld()
        {
            return "Hello World!";
        }

        public static string Echo(string message)
        {
            return message;
        }

        public static ChannelReader<int> Stream(int count)
        {
            var channel = Channel.CreateUnbounded<int>();

            Task.Run(async () =>
            {
                for (var i = 0; i < count; i++)
                {
                    await channel.Writer.WriteAsync(i);
                    await Task.Delay(100);
                }

                channel.Writer.TryComplete();
            });

            return channel.Reader;
        }

        public static ChannelReader<int> StreamException()
        {
            throw new InvalidOperationException("Error occurred while streaming.");
        }

        public static ChannelReader<string> StreamBroken() => null;

        public static ChannelReader<string> StreamEcho(ChannelReader<string> source)
        {
            var output = Channel.CreateUnbounded<string>();
            _ = Task.Run(async () => {
                while (await source.WaitToReadAsync())
                {
                    while (source.TryRead(out var item))
                    {
                        await output.Writer.WriteAsync(item);
                    }
                }
                output.Writer.TryComplete();
            });

            return output.Reader;
        }
    }

    public interface ITestHub
    {
        Task Echo(string message);
        Task Send(string message);
        Task NoClientHandler();
    }

    public class VersionHub : Hub
    {
        public string Echo(string message) => message;

        public Task NewProtocolMethodServer()
        {
            return Clients.Caller.SendAsync("NewProtocolMethodClient");
        }
    }

    [Authorize(JwtBearerDefaults.AuthenticationScheme)]
    public class HubWithAuthorization : Hub
    {
        public string Echo(string message) => TestHubMethodsImpl.Echo(message);
    }
}
