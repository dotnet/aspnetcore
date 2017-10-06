// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class TestHub : Hub
    {
        public string HelloWorld()
        {
            return "Hello World!";
        }

        public string Echo(string message)
        {
            return message;
        }

        public async Task CallEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).InvokeAsync("Echo", message);
        }

        public IObservable<int> Stream(int count)
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(1))
                             .Select((_, index) => index)
                             .Take(count);
        }
    }

    public class DynamicTestHub : DynamicHub
    {
        public string HelloWorld()
        {
            return "Hello World!";
        }

        public string Echo(string message)
        {
            return message;
        }

        public async Task CallEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).Echo(message);
        }

        public IObservable<int> Stream(int count)
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(1))
                             .Select((_, index) => index)
                             .Take(count);
        }

        public Task SendMessage(string message)
        {
            return Clients.All.Send(message);
        }
    }

    public class TestHubT : Hub<ITestHub>
    {
        public string HelloWorld()
        {
            return "Hello World!";
        }

        public string Echo(string message)
        {
            return message;
        }

        public async Task CallEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).Echo(message);
        }

        public IObservable<int> Stream(int count)
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(1))
                             .Select((_, index) => index)
                             .Take(count);
        }

        public Task SendMessage(string message)
        {
            return Clients.All.Send(message);
        }
    }

    public interface ITestHub
    {
        Task Echo(string message);
        Task Send(string message);
    }

}
