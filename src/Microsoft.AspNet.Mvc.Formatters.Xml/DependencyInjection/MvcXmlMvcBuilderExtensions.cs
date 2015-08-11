// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding XML formatters to MVC.
    /// </summary>
    public static class MvcXmlMvcBuilderExtensions
    {
        /// <summary>
        /// Adds the XML DataContractSerializer formatters to MVC.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddXmlDataContractSerializerFormatters([NotNull] this IMvcBuilder builder)
        {
            AddXmlDataContractSerializerFormatterServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Adds the XML Serializer formatters to MVC.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddXmlSerializerFormatters([NotNull] this IMvcBuilder builder)
        {
            AddXmlSerializerFormatterServices(builder.Services);
            return builder;
        }

        // Internal for testing.
        internal static void AddXmlDataContractSerializerFormatterServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcXmlDataContractSerializerMvcOptionsSetup>());
        }

        // Internal for testing.
        internal static void AddXmlSerializerFormatterServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcXmlSerializerMvcOptionsSetup>());
        }
    }
}
