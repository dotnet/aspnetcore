// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Default services
    /// </summary>
    public class IdentityEntityFrameworkServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices(Type userType, Type roleType, Type contextType, Type keyType = null, IConfiguration config = null)
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
            Type userStoreType;
            Type roleStoreType;
            if (keyType != null)
            {
                userStoreType = typeof(UserStore<,,,>).MakeGenericType(userType, roleType, contextType, keyType);
                roleStoreType = typeof(RoleStore<,,>).MakeGenericType(roleType, contextType, keyType);
            }
            else
            {
                userStoreType = typeof(UserStore<,,>).MakeGenericType(userType, roleType, contextType);
                roleStoreType = typeof(RoleStore<,>).MakeGenericType(roleType, contextType);
            }

            yield return describe.Scoped(
                typeof(IUserStore<>).MakeGenericType(userType),
                userStoreType);
            yield return describe.Scoped(
                typeof(IRoleStore<>).MakeGenericType(roleType),
                roleStoreType);
        }
    }
}