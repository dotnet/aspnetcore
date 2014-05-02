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

// <copyright file="DenyAnonymous.cs" company="Katana contributors">
//   Copyright 2011-2012 Katana contributors
// </copyright>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.Windows.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // This middleware can be placed at the end of a chain of pass-through auth schemes if at least one type of auth is required.
    public class DenyAnonymous
    {
        private readonly AppFunc _nextApp;

        public DenyAnonymous(AppFunc nextApp)
        {
            _nextApp = nextApp;
        }

        public async Task Invoke(IDictionary<string, object> env)
        {
            if (env.Get<IPrincipal>("server.User") == null)
            {
                env["owin.ResponseStatusCode"] = 401;
                return;
            }

            await _nextApp(env);
        }
    }
}
