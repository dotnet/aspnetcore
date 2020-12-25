// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// An options type for configuring the application <see cref="Mvc.CompatibilityVersion"/>.
    /// </summary>
    /// <remarks>
    /// The primary way to configure the application's <see cref="Mvc.CompatibilityVersion"/> is by
    /// calling <see cref="MvcCoreMvcBuilderExtensions.SetCompatibilityVersion(IMvcBuilder, CompatibilityVersion)"/>
    /// or <see cref="MvcCoreMvcCoreBuilderExtensions.SetCompatibilityVersion(IMvcCoreBuilder, CompatibilityVersion)"/>.
    /// </remarks>
    [Obsolete("This API is obsolete and will be removed in a future version. Consider removing usages.",
        DiagnosticId = "ASP5001",
        UrlFormat = "https://aka.ms/aspnetcore-warnings/{0}")]
    public class MvcCompatibilityOptions
    {
        /// <summary>
        /// Gets or sets the application's configured <see cref="Mvc.CompatibilityVersion"/>.
        /// </summary>
        /// <value>the default value is <see cref="CompatibilityVersion.Version_3_0"/>.</value>
        public CompatibilityVersion CompatibilityVersion { get; set; } = CompatibilityVersion.Version_3_0;
    }
}
