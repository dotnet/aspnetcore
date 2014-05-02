// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="PassThroughTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Security.Windows.Tests
{
    public class PassThroughTests
    {
        private const int DefaultStatusCode = 201;

        [Fact]
        public async Task PassThrough_EmptyRequest_Success()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(SimpleApp);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest();
            await windowsAuth.Invoke(emptyEnv);

            Assert.Equal(DefaultStatusCode, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
        }

        [Fact]
        public async Task PassThrough_BasicAuth_Success()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(SimpleApp);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest("Authorization", "Basic blablabla");
            await windowsAuth.Invoke(emptyEnv);

            Assert.Equal(DefaultStatusCode, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
        }

        private IDictionary<string, object> CreateEmptyRequest(string header = null, string value = null)
        {
            IDictionary<string, object> env = new Dictionary<string, object>();
            var requestHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            env["owin.RequestHeaders"] = requestHeaders;
            if (header != null)
            {
                requestHeaders[header] = new string[] { value };
            }
            env["owin.ResponseHeaders"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            env["server.OnSendingHeaders"] = new Action<Action<object>, object>((a, b) => { });
            return env;
        }

        private Task SimpleApp(IDictionary<string, object> env)
        {
            env["owin.ResponseStatusCode"] = DefaultStatusCode;
            return Task.FromResult<object>(null);
        }
    }
}
