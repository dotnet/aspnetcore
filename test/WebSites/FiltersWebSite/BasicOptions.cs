// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Extensions.Options;

namespace FiltersWebSite
{
    public class BasicOptions : AuthenticationOptions, IOptions<BasicOptions>
    {
        public BasicOptions()
        {
        }

        public BasicOptions Value { get { return this; } }
    }
}