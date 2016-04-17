// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for JSON in the MVC framework.
    /// </summary>
    public class MvcJsonOptions
    {
        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> that are used by this application.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; } =
            JsonSerializerSettingsProvider.CreateSerializerSettings();
    }
}