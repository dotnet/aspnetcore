// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class StartupNoServices
    {
        public StartupNoServices()
        {
        }

        public void Configure(IApplicationBuilder builder)
        {
        }
    }
}