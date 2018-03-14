// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;

namespace FunctionalTests
{
    public class CustomObject
    {
        public string Name { get; set; }

        public int Value { get; set; }
    }

    public class TestHub : Hub
    {
        public string Echo(string message)
        {
            return message;
        }

        public void ThrowException(string message)
        {
            throw new InvalidOperationException(message);
        }

        public Task InvokeWithString(string message)
        {
            return Clients.Client(Context.Connection.ConnectionId).SendAsync("Message", message);
        }

        public Task SendCustomObject(CustomObject customObject)
        {
            return Clients.Client(Context.ConnectionId).SendAsync("CustomObject", customObject);
        }

        public IObservable<string> Stream()
        {
            return new string[] { "a", "b", "c" }.ToObservable();
        }

        public IObservable<int> EmptyStream()
        {
            return Array.Empty<int>().ToObservable();
        }

        public IObservable<string> StreamThrowException(string message)
        {
            throw new InvalidOperationException(message);
        }

        public string GetActiveTransportName()
        {
            return Context.Connection.Metadata[ConnectionMetadataNames.Transport].ToString();
        }

        public ComplexObject EchoComplexObject(ComplexObject complexObject)
        {
            return complexObject;
        }
    }
}
