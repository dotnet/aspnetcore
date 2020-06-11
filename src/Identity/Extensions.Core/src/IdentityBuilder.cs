// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Identity.Core;

namespace Microsoft.AspNetCore.Identity
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
        /// <param name="services">The <see cref="IServiceCollection"/> to attach to.</param>
        public IdentityBuilder(Type user, IServiceCollection services)
        {
            UserType = user;
            Services = services;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IdentityBuilder"/>.
        /// </summary>
        /// <param name="user">The <see cref="Type"/> to use for the users.</param>
        /// <param name="role">The <see cref="Type"/> to use for the roles.</param>
        /// <param name="services">The <see cref="IServiceCollection"/> to attach to.</param>
        public IdentityBuilder(Type user, Type role, IServiceCollection services) : this(user, services)
            => RoleType = role;

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
        /// Adds an <see cref="IUserValidator{TUser}"/> for the <see cref="UserType"/>.
        /// </summary>
        /// <typeparam name="TValidator">The user validator type.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddUserValidator<TValidator>() where TValidator : class
            => AddScoped(typeof(IUserValidator<>).MakeGenericType(UserType), typeof(TValidator));

        /// <summary>
        /// Adds an <see cref="IUserClaimsPrincipalFactory{TUser}"/> for the <see cref="UserType"/>.
        /// </summary>
        /// <typeparam name="TFactory">The type of the claims principal factory.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddClaimsPrincipalFactory<TFactory>() where TFactory : class
            => AddScoped(typeof(IUserClaimsPrincipalFactory<>).MakeGenericType(UserType), typeof(TFactory));

        /// <summary>
        /// Adds an <see cref="IdentityErrorDescriber"/>.
        /// </summary>
        /// <typeparam name="TDescriber">The type of the error describer.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddErrorDescriber<TDescriber>() where TDescriber : IdentityErrorDescriber
        {
            Services.AddScoped<IdentityErrorDescriber, TDescriber>();
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IPasswordValidator{TUser}"/> for the <see cref="UserType"/>.
        /// </summary>
        /// <typeparam name="TValidator">The validator type used to validate passwords.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddPasswordValidator<TValidator>() where TValidator : class
            => AddScoped(typeof(IPasswordValidator<>).MakeGenericType(UserType), typeof(TValidator));

        /// <summary>
        /// Adds an <see cref="IUserStore{TUser}"/> for the <see cref="UserType"/>.
        /// </summary>
        /// <typeparam name="TStore">The user store type.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddUserStore<TStore>() where TStore : class
            => AddScoped(typeof(IUserStore<>).MakeGenericType(UserType), typeof(TStore));

        /// <summary>
        /// Adds a token provider.
        /// </summary>
        /// <typeparam name="TProvider">The type of the token provider to add.</typeparam>
        /// <param name="providerName">The name of the provider to add.</param>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddTokenProvider<TProvider>(string providerName) where TProvider : class
            => AddTokenProvider(providerName, typeof(TProvider));

        /// <summary>
        /// Adds a token provider for the <see cref="UserType"/>.
        /// </summary>
        /// <param name="providerName">The name of the provider to add.</param>
        /// <param name="provider">The type of the <see cref="IUserTwoFactorTokenProvider{TUser}"/> to add.</param>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddTokenProvider(string providerName, Type provider)
        {
            if (!typeof(IUserTwoFactorTokenProvider<>).MakeGenericType(UserType).GetTypeInfo().IsAssignableFrom(provider.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatInvalidManagerType(provider.Name, "IUserTwoFactorTokenProvider", UserType.Name));
            }
            Services.Configure<IdentityOptions>(options =>
            {
                options.Tokens.ProviderMap[providerName] = new TokenProviderDescriptor(provider);
            });
            Services.AddTransient(provider);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="UserManager{TUser}"/> for the <see cref="UserType"/>.
        /// </summary>
        /// <typeparam name="TUserManager">The type of the user manager to add.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddUserManager<TUserManager>() where TUserManager : class
        {
            var userManagerType = typeof(UserManager<>).MakeGenericType(UserType);
            var customType = typeof(TUserManager);
            if (!userManagerType.GetTypeInfo().IsAssignableFrom(customType.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatInvalidManagerType(customType.Name, "UserManager", UserType.Name));
            }
            if (userManagerType != customType)
            {
                Services.AddScoped(customType, services => services.GetRequiredService(userManagerType));
            }
            return AddScoped(userManagerType, customType);
        }

        /// <summary>
        /// Adds Role related services for TRole, including IRoleStore, IRoleValidator, and RoleManager.
        /// </summary>
        /// <typeparam name="TRole">The role type.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddRoles<TRole>() where TRole : class
        {
            RoleType = typeof(TRole);
            AddRoleValidator<RoleValidator<TRole>>();
            Services.TryAddScoped<RoleManager<TRole>>();
            Services.AddScoped(typeof(IUserClaimsPrincipalFactory<>).MakeGenericType(UserType), typeof(UserClaimsPrincipalFactory<,>).MakeGenericType(UserType, RoleType));
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IRoleValidator{TRole}"/> for the <see cref="RoleType"/>.
        /// </summary>
        /// <typeparam name="TRole">The role validator type.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddRoleValidator<TRole>() where TRole : class
        {
            if (RoleType == null)
            {
                throw new InvalidOperationException(Resources.NoRoleType);
            }
            return AddScoped(typeof(IRoleValidator<>).MakeGenericType(RoleType), typeof(TRole));
        }

        /// <summary>
        /// Adds an <see cref="ILookupProtector"/> and <see cref="ILookupProtectorKeyRing"/>.
        /// </summary>
        /// <typeparam name="TProtector">The personal data protector type.</typeparam>
        /// <typeparam name="TKeyRing">The personal data protector key ring type.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddPersonalDataProtection<TProtector, TKeyRing>()
            where TProtector : class,ILookupProtector
            where TKeyRing : class, ILookupProtectorKeyRing
        {
            Services.AddSingleton<IPersonalDataProtector, DefaultPersonalDataProtector>();
            Services.AddSingleton<ILookupProtector, TProtector>();
            Services.AddSingleton<ILookupProtectorKeyRing, TKeyRing>();
            return this;
        }

        /// <summary>
        /// Adds a <see cref="IRoleStore{TRole}"/> for the <see cref="RoleType"/>.
        /// </summary>
        /// <typeparam name="TStore">The role store.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddRoleStore<TStore>() where TStore : class
        {
            if (RoleType == null)
            {
                throw new InvalidOperationException(Resources.NoRoleType);
            }
            return AddScoped(typeof(IRoleStore<>).MakeGenericType(RoleType), typeof(TStore));
        }

        /// <summary>
        /// Adds a <see cref="RoleManager{TRole}"/> for the <see cref="RoleType"/>.
        /// </summary>
        /// <typeparam name="TRoleManager">The type of the role manager to add.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddRoleManager<TRoleManager>() where TRoleManager : class
        {
            if (RoleType == null)
            {
                throw new InvalidOperationException(Resources.NoRoleType);
            }
            var managerType = typeof(RoleManager<>).MakeGenericType(RoleType);
            var customType = typeof(TRoleManager);
            if (!managerType.GetTypeInfo().IsAssignableFrom(customType.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatInvalidManagerType(customType.Name, "RoleManager", RoleType.Name));
            }
            if (managerType != customType)
            {
                Services.AddScoped(typeof(TRoleManager), services => services.GetRequiredService(managerType));
            }
            return AddScoped(managerType, typeof(TRoleManager));
        }

        /// <summary>
        /// Adds a <see cref="IUserConfirmation{TUser}"/> for the <seealso cref="UserType"/>.
        /// </summary>
        /// <typeparam name="TUserConfirmation">The type of the user confirmation to add.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual IdentityBuilder AddUserConfirmation<TUserConfirmation>() where TUserConfirmation : class
            => AddScoped(typeof(IUserConfirmation<>).MakeGenericType(UserType), typeof(TUserConfirmation));
    }
}
