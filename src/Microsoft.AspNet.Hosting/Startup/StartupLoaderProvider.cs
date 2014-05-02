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

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoaderProvider : IStartupLoaderProvider
    {
        private readonly IServiceProvider _services;

        public StartupLoaderProvider(IServiceProvider services)
        {
            _services = services;
        }

        public int Order { get { return -100; } }

        public IStartupLoader CreateStartupLoader(IStartupLoader next)
        {
            return new StartupLoader(_services, next);
        }
    }
}
