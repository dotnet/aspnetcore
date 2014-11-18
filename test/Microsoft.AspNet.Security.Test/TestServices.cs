// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Security
{
    public static class TestServices
    {
        public static IServiceProvider CreateTestServices()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>();
            collection.Add(DataProtectionServices.GetDefaultServices());
            return collection.BuildServiceProvider();
        }
    }
}