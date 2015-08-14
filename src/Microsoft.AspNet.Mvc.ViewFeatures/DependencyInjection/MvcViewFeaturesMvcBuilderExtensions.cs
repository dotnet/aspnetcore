// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
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
        /// <param name="setupAction">The <see cref="MvcViewOptions"/> which need to be configured.</param>
        public static IMvcBuilder AddViewOptions(
            [NotNull] this IMvcBuilder builder,
            [NotNull] Action<MvcViewOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
            return builder;
        }
    }
}
