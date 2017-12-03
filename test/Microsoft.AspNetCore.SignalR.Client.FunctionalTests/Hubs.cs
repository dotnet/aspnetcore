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

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class TestHub : Hub
    {
        public string HelloWorld() => TestHubMethodsImpl.HelloWorld();

        public string Echo(string message) => TestHubMethodsImpl.Echo(message);

        public IObservable<int> Stream(int count) => TestHubMethodsImpl.Stream(count);

        public ChannelReader<int> StreamException() => TestHubMethodsImpl.StreamException();

        public ChannelReader<string> StreamBroken() => TestHubMethodsImpl.StreamBroken();

        public async Task CallEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).InvokeAsync("Echo", message);
        }

        public async Task CallHandlerThatDoesntExist()
        {
            await Clients.Client(Context.ConnectionId).InvokeAsync("NoClientHandler");
        }

        public IEnumerable<string> GetHeaderValues(string[] headerNames)
        {
            var headers = Context.Connection.GetHttpContext().Request.Headers;
            return headerNames.Select(h => (string)headers[h]);
        }

        public string GetCookieValue(string cookieName)
        {
            return Context.Connection.GetHttpContext().Request.Cookies[cookieName];
        }
    }

    public class DynamicTestHub : DynamicHub
    {
        public string HelloWorld() => TestHubMethodsImpl.HelloWorld();

        public string Echo(string message) => TestHubMethodsImpl.Echo(message);

        public IObservable<int> Stream(int count) => TestHubMethodsImpl.Stream(count);

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
    }

    public class TestHubT : Hub<ITestHub>
    {
        public string HelloWorld() => TestHubMethodsImpl.HelloWorld();

        public string Echo(string message) => TestHubMethodsImpl.Echo(message);

        public IObservable<int> Stream(int count) => TestHubMethodsImpl.Stream(count);

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

        public static IObservable<int> Stream(int count)
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(1))
                             .Select((_, index) => index)
                             .Take(count);
        }

        public static ChannelReader<int> StreamException()
        {
            throw new InvalidOperationException("Error occurred while streaming.");
        }

        public static ChannelReader<string> StreamBroken() => null;
    }

    public interface ITestHub
    {
        Task Echo(string message);
        Task Send(string message);
        Task NoClientHandler();
    }

    [Authorize(JwtBearerDefaults.AuthenticationScheme)]
    public class HubWithAuthorization : Hub
    {
        public string Echo(string message) => TestHubMethodsImpl.Echo(message);
    }
}
