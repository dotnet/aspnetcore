// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public static IEnumerable<IServiceDescriptor> GetDefaultUserServices<TUser>() where TUser : class
        {
            return GetDefaultUserServices<TUser>(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultUserServices<TUser>(IConfiguration configuration)
            where TUser : class
        {
            var describe = new ServiceDescriber(configuration);

            yield return describe.Transient<IUserValidator<TUser>, UserValidator<TUser>>();
            yield return describe.Transient<IPasswordValidator<TUser>, PasswordValidator<TUser>>();
            yield return describe.Transient<IPasswordHasher, PasswordHasher>();

            // TODO: rationalize email/sms/usertoken services
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultRoleServices<TRole>() where TRole : class
        {
            return GetDefaultRoleServices<TRole>(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultRoleServices<TRole>(IConfiguration configuration)
            where TRole : class
        {
            var describe = new ServiceDescriber(configuration);
            yield return describe.Instance<IRoleValidator<TRole>>(new RoleValidator<TRole>());
        }
    }
}