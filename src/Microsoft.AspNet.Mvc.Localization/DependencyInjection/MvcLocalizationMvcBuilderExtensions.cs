// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring MVC view localization.
    /// </summary>
    public static class MvcLocalizationMvcBuilderExtensions
    {
        /// <summary>
        /// Adds MVC localization to the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        /// <remarks>
        /// Adding localization also adds support for views via
        /// <see cref="MvcViewFeaturesMvcBuilderExtensions.AddViews(IMvcBuilder)"/> and the Razor view engine
        /// via <see cref="MvcRazorMvcBuilderExtensions.AddRazorViewEngine(IMvcBuilder)"/>.
        /// </remarks>
        public static IMvcBuilder AddLocalization([NotNull] this IMvcBuilder builder)
        {
            return AddLocalization(builder, LanguageViewLocationExpanderFormat.Suffix);
        }

        /// <summary>
        ///  Adds MVC localization to the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="format">The view format for localized views.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        /// <remarks>
        /// Adding localization also adds support for views via
        /// <see cref="MvcViewFeaturesMvcBuilderExtensions.AddViews(IMvcBuilder)"/> and the Razor view engine
        /// via <see cref="MvcRazorMvcBuilderExtensions.AddRazorViewEngine(IMvcBuilder)"/>.
        /// </remarks>
        public static IMvcBuilder AddLocalization(
            [NotNull] this IMvcBuilder builder,
            LanguageViewLocationExpanderFormat format)
        {

            builder.AddViews();
            builder.AddRazorViewEngine();

            MvcLocalizationServiceCollectionExtensions.AddMvcLocalization(builder.Services, format);
            return builder;
        }
    }
}