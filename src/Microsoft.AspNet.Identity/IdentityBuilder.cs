// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using System;

namespace Microsoft.AspNet.Identity
{
    public class IdentityBuilder<TUser, TRole> where TUser : class where TRole : class
    {
        public ServiceCollection Services { get; private set; }

        public IdentityBuilder(ServiceCollection services)
        {
            Services = services;
        }

        // Rename to Add

        public IdentityBuilder<TUser, TRole> AddInstance<T>(Func<T> func)
        {
            Services.AddInstance(func());
            return this;
        }

        public IdentityBuilder<TUser, TRole> AddUserStore(Func<IUserStore<TUser>> func)
        {
            return AddInstance(func);
        }

        public IdentityBuilder<TUser, TRole> AddRoleStore(Func<IRoleStore<TRole>> func)
        {
            return AddInstance(func);
        }

        public IdentityBuilder<TUser, TRole> AddPasswordValidator(Func<IPasswordValidator<TUser>> func)
        {
            return AddInstance(func);
        }

        public IdentityBuilder<TUser, TRole> AddUserValidator(Func<IUserValidator<TUser>> func)
        {
            return AddInstance(func);
        }

        public class OptionsSetup<TOptions> : IOptionsSetup<TOptions>
        {
            public Action<TOptions> SetupAction { get; private set; }

            public OptionsSetup(Action<TOptions> setupAction)
            {
                if (setupAction == null)
                {
                    throw new ArgumentNullException("setupAction");
                }
                SetupAction = setupAction;
            }

            public void Setup(TOptions options)
            {
                SetupAction(options);
            }

            public int Order { get; set; }
        }

        public IdentityBuilder<TUser, TRole> SetupOptions(Action<IdentityOptions> action, int order)
        {
            Services.AddSetup(new OptionsSetup<IdentityOptions>(action) {Order = order});
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

        //public IdentityBuilder<TUser, TRole> UseTwoFactorProviders(Func<IDictionary<string, IUserTokenProvider<TUser>>> func)
        //{
        //    return Use(func);
        //}

    }
}