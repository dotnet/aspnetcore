// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity.Test
{
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IServiceProvider services, IUserStore<ApplicationUser> store, IOptionsAccessor<IdentityOptions> options) : base(services, store, options) { }
    }

    public class ApplicationRoleManager : RoleManager<IdentityRole>
    {
        public ApplicationRoleManager(IServiceProvider services, IRoleStore<IdentityRole> store) : base(services, store) { }
    }

    public class ApplicationUser : IdentityUser
    {
    }
}