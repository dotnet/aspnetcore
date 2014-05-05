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

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Hosting
{
    public class PipelineInstance : IDisposable
    {
        private readonly IHttpContextFactory _httpContextFactory;
        private readonly RequestDelegate _requestDelegate;

        public PipelineInstance(IHttpContextFactory httpContextFactory, RequestDelegate requestDelegate)
        {
            _httpContextFactory = httpContextFactory;
            _requestDelegate = requestDelegate;
        }

        public Task Invoke(object serverEnvironment)
        {
            var httpContext = _httpContextFactory.CreateHttpContext(serverEnvironment);
            return _requestDelegate(httpContext);
        }

        public void Dispose()
        {
            // TODO: application notification of disposal
        }
    }
}
