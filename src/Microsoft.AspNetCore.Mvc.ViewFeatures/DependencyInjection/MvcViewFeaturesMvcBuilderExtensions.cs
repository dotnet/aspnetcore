// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions methods for configuring MVC via an <see cref="IMvcBuilder"/>.
    /// </summary>
    public static class MvcViewFeaturesMvcBuilderExtensions
    {
        /// <summary>
        /// Adds configuration of <see cref="MvcViewOptions"/> for the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">
        /// An <see cref="Action{MvcViewOptions}"/> to configure the provided <see cref="MvcViewOptions"/>.
        /// </param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddViewOptions(
            this IMvcBuilder builder,
            Action<MvcViewOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure(setupAction);
            return builder;
        }

        /// <summary>
        /// Registers discovered view components as services in the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddViewComponentsAsServices(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var feature = new ViewComponentFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (var viewComponent in feature.ViewComponents.Select(vc => vc.AsType()))
            {
                builder.Services.TryAddTransient(viewComponent, viewComponent);
            }

            builder.Services.Replace(ServiceDescriptor.Singleton<IViewComponentActivator, ServiceBasedViewComponentActivator>());

            return builder;
        }

        /// <summary>
        /// Registers <see cref="SessionStateTempDataProvider"/> as the default <see cref="ITempDataProvider"/>
        /// in the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddSessionStateTempDataProvider(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Ensure the TempData basics are registered.
            MvcViewFeaturesMvcCoreBuilderExtensions.AddViewServices(builder.Services);

            var descriptor = ServiceDescriptor.Singleton(typeof(ITempDataProvider), typeof(SessionStateTempDataProvider));
            builder.Services.Replace(descriptor);

            return builder;
        }

        /// <summary>
        /// Registers <see cref="CookieTempDataProvider"/> as the default <see cref="ITempDataProvider"/> in the
        /// <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddCookieTempDataProvider(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Ensure the TempData basics are registered.
            MvcViewFeaturesMvcCoreBuilderExtensions.AddViewServices(builder.Services);

            var descriptor = ServiceDescriptor.Singleton(typeof(ITempDataProvider), typeof(CookieTempDataProvider));
            builder.Services.Replace(descriptor);

            return builder;
        }

        /// <summary>
        /// Registers <see cref="CookieTempDataProvider"/> as the default <see cref="ITempDataProvider"/> in the
        /// <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">
        /// An <see cref="Action{CookieTempDataProviderOptions}"/> to configure the provided
        /// <see cref="CookieTempDataProviderOptions"/>.
        /// </param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddCookieTempDataProvider(
            this IMvcBuilder builder,
            Action<CookieTempDataProviderOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddCookieTempDataProvider(builder);
            builder.Services.Configure(setupAction);

            return builder;
        }
    }
}
