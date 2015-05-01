// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures a set of <see cref="RazorViewEngineOptions"/> for the application.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">An action to configure the <see cref="RazorViewEngineOptions"/>.</param>
        public static void ConfigureRazorViewEngine(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<RazorViewEngineOptions> setupAction)
        {
            services.Configure(setupAction);
        }

        /// <summary>
        /// Adds an initialization callback for a given <typeparamref name="TTagHelper"/>.
        /// </summary>
        /// <remarks>
        /// The callback will be invoked on any <typeparamref name="TTagHelper"/> instance before the
        /// <see cref="ITagHelper.ProcessAsync(TagHelperContext, TagHelperOutput)"/> method is called.
        /// </remarks>
        /// <typeparam name="TTagHelper">The type of <see cref="ITagHelper"/> being initialized.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> instance this method extends.</param>
        /// <param name="initialize">An action to initialize the <typeparamref name="TTagHelper"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance this method extends.</returns>
        public static IServiceCollection InitializeTagHelper<TTagHelper>(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<TTagHelper, ViewContext> initialize)
            where TTagHelper : ITagHelper
        {
            var initializer = new TagHelperInitializer<TTagHelper>(initialize);

            services.AddInstance(typeof(ITagHelperInitializer<TTagHelper>), initializer);

            return services;
        }
    }
}