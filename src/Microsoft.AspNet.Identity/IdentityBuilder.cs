// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Helper functions for configuring identity services.
    /// </summary>
    public class IdentityBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="IdentityBuilder"/>.
        /// </summary>
        /// <param name="user">The <see cref="Type"/> to use for the users.</param>
        /// <param name="role">The <see cref="Type"/> to use for the roles.</param>
        /// <param name="services">The <see cref="IServiceCollection"/> to attach to.</param>
        public IdentityBuilder(Type user, Type role, IServiceCollection services)
        {
            UserType = user;
            RoleType = role;
            Services = services;
        }

        /// <summary>
        /// Gets the <see cref="Type"/> used for users.
        /// </summary>
        /// <value>
        /// The <see cref="Type"/> used for users.
        /// </value>
        public Type UserType { get; private set; }


        /// <summary>
        /// Gets the <see cref="Type"/> used for roles.
        /// </summary>
        /// <value>
        /// The <see cref="Type"/> used for roles.
        /// </value>
        public Type RoleType { get; private set; }

        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> services are attached to.
        /// </summary>
        /// <value>
        /// The <see cref="IServiceCollection"/> services are attached to.
        /// </value>
        public IServiceCollection Services { get; private set; }

        private IdentityBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IUserValidator"/> for the <seealso cref="UserType"/>.
        /// </summary>
        /// <typeparam name="T">The user type to validate.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddUserValidator<TValidator>() where TValidator : class, IUserValidator
        {
            Services.AddScoped<IUserValidator, TValidator>();
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IRoleValidator{TRole}"/> for the <seealso cref="RoleType"/>.
        /// </summary>
        /// <typeparam name="T">The role type to validate.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddRoleValidator<TValidator>() where TValidator : class, IRoleValidator
        {
            Services.AddScoped<IRoleValidator, TValidator>();
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IdentityErrorDescriber"/>.
        /// </summary>
        /// <typeparam name="T">The type of the error describer.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddErrorDescriber<TDescriber>() where TDescriber : IdentityErrorDescriber
        {
            Services.AddScoped<IdentityErrorDescriber, TDescriber>();
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IPasswordValidator{TUser}"/> for the <seealso cref="UserType"/>.
        /// </summary>
        /// <typeparam name="T">The user type whose password will be validated.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddPasswordValidator<TValidator>() where TValidator : class,IPasswordValidator
        {
            Services.AddScoped<IPasswordValidator, TValidator>();
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IUserStore{TUser}"/> for the <seealso cref="UserType"/>.
        /// </summary>
        /// <typeparam name="T">The user type whose password will be validated.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddUserStore<T>() where T : class
        {
            return AddScoped(typeof(IUserStore<>).MakeGenericType(UserType), typeof(T));
        }

        /// <summary>
        /// Adds a <see cref="IRoleStore{TRole}"/> for the <seealso cref="RoleType"/>.
        /// </summary>
        /// <typeparam name="T">The role type held in the store.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddRoleStore<T>() where T : class
        {
            return AddScoped(typeof(IRoleStore<>).MakeGenericType(RoleType), typeof(T));
        }

        /// <summary>
        /// Adds a token provider.
        /// </summary>
        /// <typeparam name="TProvider">The type of the token provider to add.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddTokenProvider<TProvider>() where TProvider : class, IUserTokenProvider
        {
            Services.AddScoped<IUserTokenProvider, TProvider>();
            return this;
        }

        /// <summary>
        /// Adds the default token providers.
        /// </summary>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddDefaultTokenProviders()
        {
            Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.Name = Resources.DefaultTokenProvider;
            });

            return AddTokenProvider<DataProtectorTokenProvider>()
                .AddTokenProvider<PhoneNumberTokenProvider>()
                .AddTokenProvider<EmailTokenProvider>();
        }

        /// <summary>
        /// Adds a <see cref="UserManager{TUser}"/> for the <seealso cref="UserType"/>.
        /// </summary>
        /// <typeparam name="TUserManager">The type of the user manager to add.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddUserManager<TUserManager>() where TUserManager : class
        {
            return AddScoped(typeof(UserManager<>).MakeGenericType(UserType), typeof(TUserManager));
        }

        /// <summary>
        /// Adds a <see cref="RoleManager{TRole}"/> for the <seealso cref="RoleType"/>.
        /// </summary>
        /// <typeparam name="TRoleManager">The type of the role manager to add.</typeparam>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public virtual IdentityBuilder AddRoleManager<TRoleManager>() where TRoleManager : class
        {
            return AddScoped(typeof(RoleManager<>).MakeGenericType(RoleType), typeof(TRoleManager));
        }
    }
}