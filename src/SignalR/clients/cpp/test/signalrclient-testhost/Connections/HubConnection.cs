// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace SelfHost
{
    public class HubConnection : Hub
    {
        public IEnumerable<int> ForceReconnect()
        {
            yield return 1;
            // throwing here will close the websocket which should trigger reconnect
            throw new Exception();
        }

        public void InvokeWithString(string message)
        {
            Clients.Caller.sendString("Send: " + message);
        }

        public string ReturnString(string message)
        {
            return message;
        }

        public void InvokeWithEmptyParam()
        {
            Clients.Caller.sendString("Send");
        }

        public void InvokeWithPrimitiveParams(int myint, float myfloat, double mydouble, bool mybool, char mychar)
        {
            Clients.Caller.sendPrimitiveParams(myint + 1, myfloat + 1, mydouble + 1, mybool, mychar);
        }

        public void InvokeWithComplexType(Person p)
        {
            Clients.Caller.sendComplexType(p);
        }

        public Person ReturnComplexType(Person p)
        {
            return p;
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public Address Address { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public string Zip { get; set; }
        }

        public async Task InvokeWithProgress(IProgress<int> progress)
        {
            for (int i = 0; i < 5; i++)
            {
                progress.Report(i);
                await Task.Delay(10);
            }
        }

        public async Task<string> InvokeWithProgress(string jobName, IProgress<int> progress)
        {
            for (int i = 0; i < 5; i++)
            {
                progress.Report(i);
                await Task.Delay(10);

            }

            return string.Format("{0} done!", jobName);
        }

        public string MirrorHeader()
        {
            var mirrorValue = Context.Request.Headers["x-mirror"];
            return mirrorValue;
        }
    }
}