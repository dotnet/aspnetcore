// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace MockHostTypes
{
    public class ServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => null;
    }
}
