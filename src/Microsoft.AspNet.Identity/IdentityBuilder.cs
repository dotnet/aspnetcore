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

        private IdentityBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }

        public virtual IdentityBuilder AddUserValidator<T>() where T : class
        {
            return AddScoped(typeof(IUserValidator<>).MakeGenericType(UserType), typeof(T));
        }

        public virtual IdentityBuilder AddRoleValidator<T>() where T : class
        {
            return AddScoped(typeof(IRoleValidator<>).MakeGenericType(RoleType), typeof(T));
        }

        public virtual IdentityBuilder AddErrorDescriber<TDescriber>() where TDescriber : IdentityErrorDescriber
        {
            Services.AddScoped<IdentityErrorDescriber, TDescriber>();
            return this;
        }

        public virtual IdentityBuilder AddPasswordValidator<T>() where T : class
        {
            return AddScoped(typeof(IPasswordValidator<>).MakeGenericType(UserType), typeof(T));
        }

        public virtual IdentityBuilder AddUserStore<T>() where T : class
        {
            return AddScoped(typeof(IUserStore<>).MakeGenericType(UserType), typeof(T));
        }

        public virtual IdentityBuilder AddRoleStore<T>() where T : class
        {
            return AddScoped(typeof(IRoleStore<>).MakeGenericType(RoleType), typeof(T));
        }

        public virtual IdentityBuilder AddTokenProvider<TProvider>() where TProvider : class
        {
            return AddTokenProvider(typeof(TProvider));
        }

        public virtual IdentityBuilder AddTokenProvider(Type provider)
        {
            return AddScoped(typeof(IUserTokenProvider<>).MakeGenericType(UserType), provider);
        }

        public virtual IdentityBuilder AddDefaultTokenProviders()
        {
            Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.Name = Resources.DefaultTokenProvider;
            });

            return AddTokenProvider(typeof(DataProtectorTokenProvider<>).MakeGenericType(UserType))
                .AddTokenProvider(typeof(PhoneNumberTokenProvider<>).MakeGenericType(UserType))
                .AddTokenProvider(typeof(EmailTokenProvider<>).MakeGenericType(UserType));
        }

        public virtual IdentityBuilder AddUserManager<TUserManager>() where TUserManager : class
        {
            return AddScoped(typeof(UserManager<>).MakeGenericType(UserType), typeof(TUserManager));
        }

        public virtual IdentityBuilder AddRoleManager<TRoleManager>() where TRoleManager : class
        {
            return AddScoped(typeof(RoleManager<>).MakeGenericType(RoleType), typeof(TRoleManager));
        }
    }
}