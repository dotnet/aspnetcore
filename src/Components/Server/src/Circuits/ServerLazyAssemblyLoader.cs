// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Server
{
    public class ServerLazyAssemblyLoader : ILazyLoader
    {
        public ServerLazyAssemblyLoader() { }

        public Task<IEnumerable<Assembly>> LoadAssembliesAsync(IEnumerable<string> assembliesToLoad)
        {
            return Task.FromResult<IEnumerable<Assembly>>(new List<Assembly>());
        }
    }
}
