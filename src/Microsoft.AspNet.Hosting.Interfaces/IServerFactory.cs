// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting.Server
{
    [AssemblyNeutral]
    public interface IServerFactory
    {
        IServerInformation Initialize(IConfiguration configuration);
        IDisposable Start(IServerInformation serverInformation, Func<object, Task> application);
    }
}
