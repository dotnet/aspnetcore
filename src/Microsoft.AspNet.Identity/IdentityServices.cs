// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        public static IEnumerable<IServiceDescriptor> GetDefaultUserServices<TUser>(IConfiguration configuration) where TUser : class
        {
            var describe = new ServiceDescriber(configuration);

            // TODO: review defaults for validators should get picked up from config?
            yield return describe.Transient<IUserValidator<TUser>, UserValidator<TUser>>();
            yield return describe.Transient<IPasswordValidator<TUser>, PasswordValidator<TUser>>();
            yield return describe.Transient<IPasswordHasher, PasswordHasher>();
            yield return describe.Transient<IClaimsIdentityFactory<TUser>, ClaimsIdentityFactory<TUser>>();

            // TODO: rationalize email/sms/usertoken services
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultRoleServices<TRole>() where TRole : class
        {
            return GetDefaultRoleServices<TRole>(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultRoleServices<TRole>(IConfiguration configuration) where TRole : class
        {
            var describe = new ServiceDescriber(configuration);

            // TODO: review defaults for validators should get picked up from config?
            yield return describe.Instance<IRoleValidator<TRole>>(new RoleValidator<TRole>());
        }
    }
}