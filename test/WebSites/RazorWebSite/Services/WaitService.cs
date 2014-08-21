// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace RazorWebSite
{
    public class WaitService
    {
        private static readonly TimeSpan _waitTime = TimeSpan.FromSeconds(10);
        private readonly ManualResetEventSlim _serverResetEvent = new ManualResetEventSlim();
        private readonly ManualResetEventSlim _clientResetEvent = new ManualResetEventSlim();

        public void NotifyClient()
        {
            _clientResetEvent.Set();
        }

        public void WaitForClient()
        {
            _clientResetEvent.Set();
            _serverResetEvent.Wait(_waitTime);
            _serverResetEvent.Reset();
        }

        public void WaitForServer()
        {
            _serverResetEvent.Set();
            _clientResetEvent.Wait(_waitTime);
            _clientResetEvent.Reset();
        }
    }
}