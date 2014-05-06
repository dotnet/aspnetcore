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
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplication
    {
        public static IDisposable Start()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());

            var context = new HostingContext
            {
                Services = serviceCollection.BuildServiceProvider()
            };

            var engine = context.Services.GetService<IHostingEngine>();
            if (engine == null)
            {
                throw new Exception("TODO: IHostingEngine service not available exception");
            }

            return engine.Start(context);
        }
    }
}