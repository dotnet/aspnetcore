// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Http.Core
{
    public class ServiceProvidersFeature : IServiceProvidersFeature
    {
        public IServiceProvider ApplicationServices { get; set; }
        public IServiceProvider RequestServices { get; set; }
    }
}