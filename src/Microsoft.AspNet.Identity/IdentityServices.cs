// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Default services used by UserManager and RoleManager
    /// </summary>
    public class IdentityServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices<TUser, TRole>(IConfiguration config = null)
            where TUser : class where TRole : class
        {
            ServiceDescriber describe;
            if (config == null)
            {
                describe = new ServiceDescriber();
            }
            else
            {
                describe = new ServiceDescriber(config);
            }
            yield return describe.Transient<IUserValidator<TUser>, UserValidator<TUser>>();
            yield return describe.Transient<IPasswordValidator<TUser>, PasswordValidator<TUser>>();
            yield return describe.Transient<IPasswordHasher<TUser>, PasswordHasher<TUser>>();
            yield return describe.Transient<IUserNameNormalizer, UpperInvariantUserNameNormalizer>();
            yield return describe.Transient<IRoleValidator<TRole>, RoleValidator<TRole>>();
            yield return describe.Scoped<ISecurityStampValidator, SecurityStampValidator<TUser>>();
            yield return describe.Scoped<IClaimsIdentityFactory<TUser>, ClaimsIdentityFactory<TUser, TRole>>();
            yield return describe.Scoped<UserManager<TUser>, UserManager<TUser>>();
            yield return describe.Scoped<SignInManager<TUser>, SignInManager<TUser>>();
            yield return describe.Scoped<RoleManager<TRole>, RoleManager<TRole>>();
        }
    }
}