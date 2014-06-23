// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity.Test
{
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store, IOptionsAccessor<IdentityOptions> options,
            IPasswordHasher passwordHasher, IUserValidator<ApplicationUser> userValidator,
            IPasswordValidator<ApplicationUser> passwordValidator)
            : base(store, options, passwordHasher, userValidator, passwordValidator) { }
    }

    public class ApplicationRoleManager : RoleManager<IdentityRole>
    {
        public ApplicationRoleManager(IRoleStore<IdentityRole> store, IRoleValidator<IdentityRole> roleValidator)
            : base(store, roleValidator) { }
    }

    public class ApplicationUser : IdentityUser
    {
    }
}