// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.Framework.OptionsModel;

namespace FiltersWebSite
{
    public class BasicOptions : AuthenticationOptions
    {
        public BasicOptions()
        {
            AuthenticationMode = AuthenticationMode.Passive;
        }
    }
}