// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    public class IdentityBuilder<TUser, TRole> where TUser : class where TRole : class
    {
        public IServiceCollection Services { get; private set; }

        public IdentityBuilder(IServiceCollection services)
        {
            Services = services;
        }

        // Rename to Add

        public IdentityBuilder<TUser, TRole> AddInstance<T>(T obj)
        {
            Services.AddInstance(obj);
            return this;
        }

        public IdentityBuilder<TUser, TRole> AddUserStore(IUserStore<TUser> store)
        {
            return AddInstance(store);
        }

        public IdentityBuilder<TUser, TRole> AddRoleStore(IRoleStore<TRole> store)
        {
            return AddInstance(store);
        }

        public IdentityBuilder<TUser, TRole> AddPasswordValidator(IPasswordValidator<TUser> validator)
        {
            return AddInstance(validator);
        }

        public IdentityBuilder<TUser, TRole> AddUserValidator(IUserValidator<TUser> validator)
        {
            return AddInstance(validator);
        }

        public IdentityBuilder<TUser, TRole> AddTokenProvider(IUserTokenProvider<TUser> tokenProvider)
        {
            return AddInstance(tokenProvider);
        }

        public IdentityBuilder<TUser, TRole> SetupOptions(Action<IdentityOptions> action, int order)
        {
            Services.AddSetup(new OptionsSetup<IdentityOptions>(action) { Order = order });
            return this;
        }

        public IdentityBuilder<TUser, TRole> SetupOptions(Action<IdentityOptions> action)
        {
            return SetupOptions(action, 0);
        }

        public IdentityBuilder<TUser, TRole> AddUserManager<TManager>() where TManager : UserManager<TUser>
        {
            Services.AddScoped<TManager>();
            return this;
        }

        public IdentityBuilder<TUser, TRole> AddRoleManager<TManager>() where TManager : RoleManager<TRole>
        {
            Services.AddScoped<TManager>();
            return this;
        }
    }
}