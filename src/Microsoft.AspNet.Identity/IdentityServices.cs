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

using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using System;
using System.Collections.Generic;

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
            yield return describe.Instance<IUserValidator<TUser>>(new UserValidator<TUser>());
            yield return describe.Instance<IPasswordValidator>(new PasswordValidator()
            {
                RequiredLength = 6,
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonLetterOrDigit = true,
                RequireUppercase = true
            });
            yield return describe.Instance<IPasswordHasher>(new PasswordHasher());
            yield return describe.Instance<IClaimsIdentityFactory<TUser>>(new ClaimsIdentityFactory<TUser>());
            yield return describe.Instance<LockoutPolicy>(new LockoutPolicy
            {
                UserLockoutEnabledByDefault = false,
                DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5),
                MaxFailedAccessAttemptsBeforeLockout = 5
            });

            // TODO: rationalize email/sms/usertoken services
            // TODO: configure lockout from config?
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