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
using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class NullStartupLoader : IStartupLoader
    {
        static NullStartupLoader()
        {
            Instance = new NullStartupLoader();
        }

        public static IStartupLoader Instance { get; private set; }

        public Action<IBuilder> LoadStartup(string applicationName, IList<string> diagnosticMessages)
        {
            return null;
        }
    }
}