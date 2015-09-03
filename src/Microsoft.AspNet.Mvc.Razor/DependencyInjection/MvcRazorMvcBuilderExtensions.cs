// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extensions methods for configuring MVC via an <see cref="IMvcBuilder"/>.
    /// </summary>
    public static class MvcRazorMvcBuilderExtensions
    {
        /// <summary>
        /// Configures a set of <see cref="RazorViewEngineOptions"/> for the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">An action to configure the <see cref="RazorViewEngineOptions"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddRazorOptions(
            [NotNull] this IMvcBuilder builder,
            [NotNull] Action<RazorViewEngineOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
            return builder;
        }

        /// <summary>
        /// Adds an initialization callback for a given <typeparamref name="TTagHelper"/>.
        /// </summary>
        /// <remarks>
        /// The callback will be invoked on any <typeparamref name="TTagHelper"/> instance before the
        /// <see cref="ITagHelper.ProcessAsync(TagHelperContext, TagHelperOutput)"/> method is called.
        /// </remarks>
        /// <typeparam name="TTagHelper">The type of <see cref="ITagHelper"/> being initialized.</typeparam>
        /// <param name="builder">The <see cref="IMvcBuilder"/> instance this method extends.</param>
        /// <param name="initialize">An action to initialize the <typeparamref name="TTagHelper"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/> instance this method extends.</returns>
        public static IMvcBuilder InitializeTagHelper<TTagHelper>(
            [NotNull] this IMvcBuilder builder,
            [NotNull] Action<TTagHelper, ViewContext> initialize)
            where TTagHelper : ITagHelper
        {
            var initializer = new TagHelperInitializer<TTagHelper>(initialize);

            builder.Services.AddInstance(typeof(ITagHelperInitializer<TTagHelper>), initializer);

            return builder;
        }

        public static IMvcBuilder AddPrecompiledRazorViews(		
            [NotNull] this IMvcBuilder builder,		
            [NotNull] params Assembly[] assemblies)		
        {		
            builder.Services.Replace(		
                ServiceDescriptor.Singleton<ICompilerCacheProvider>(serviceProvider =>		
                    ActivatorUtilities.CreateInstance<PrecompiledViewsCompilerCacheProvider>(		
                        serviceProvider,		
                        assemblies.AsEnumerable())));		
		
            return builder;		
        }		
    }
}
