// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public class IdentityBuilder
    {
        public IdentityBuilder(Type user, Type role, IServiceCollection services)
        {
            UserType = user;
            RoleType = role;
            Services = services;
        }

        public Type UserType { get; private set; }
        public Type RoleType { get; private set; }
        public IServiceCollection Services { get; private set; }

        public IdentityBuilder AddTokenProvider(Type provider)
        {
            Services.AddScoped(typeof(IUserTokenProvider<>).MakeGenericType(UserType), provider);
            return this;
        }

        public IdentityBuilder AddDefaultTokenProviders()
        {
            Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.Name = Resources.DefaultTokenProvider;
            });

            return AddTokenProvider(typeof(DataProtectorTokenProvider<>).MakeGenericType(UserType))
                .AddTokenProvider(typeof(PhoneNumberTokenProvider<>).MakeGenericType(UserType))
                .AddTokenProvider(typeof(EmailTokenProvider<>).MakeGenericType(UserType));
        }

    }

    public class IdentityBuilder<TUser, TRole> : IdentityBuilder where TUser : class where TRole : class
    {
        public IdentityBuilder(IServiceCollection services) : base(typeof(TUser), typeof(TRole), services) { }

        public IdentityBuilder<TUser, TRole> AddInstance<TService>(TService instance)
            where TService : class
        {
            Services.AddInstance(instance);
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

        public IdentityBuilder<TUser, TRole> AddTokenProvider<TTokenProvider>() where TTokenProvider : IUserTokenProvider<TUser>
        {
            Services.AddScoped<IUserTokenProvider<TUser>, TTokenProvider>();
            return this;
        }

        public IdentityBuilder<TUser, TRole> ConfigureIdentity(Action<IdentityOptions> action, int order = 0)
        {
            Services.Configure(action, order);
            return this;
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