// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding XML formatters to MVC.
    /// </summary>
    public static class MvcXmlMvcBuilderExtensions
    {
        /// <summary>
        /// Adds configuration of <see cref="MvcXmlOptions"/> for the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">The <see cref="MvcXmlOptions"/> which need to be configured.</param>
        public static IMvcBuilder AddXmlOptions(
            this IMvcBuilder builder,
            Action<MvcXmlOptions> setupAction)
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
        /// Adds the XML DataContractSerializer formatters to MVC.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddXmlDataContractSerializerFormatters(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddXmlDataContractSerializerFormatterServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Adds the XML DataContractSerializer formatters to MVC.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">The <see cref="MvcXmlOptions"/> which need to be configured.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddXmlDataContractSerializerFormatters(
            this IMvcBuilder builder,
            Action<MvcXmlOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddXmlDataContractSerializerFormatterServices(builder.Services);
            builder.Services.Configure(setupAction);
            return builder;
        }

        /// <summary>
        /// Adds the XML Serializer formatters to MVC.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddXmlSerializerFormatters(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddXmlSerializerFormatterServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Adds the XML Serializer formatters to MVC.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">The <see cref="MvcXmlOptions"/> which need to be configured.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddXmlSerializerFormatters(
            this IMvcBuilder builder,
            Action<MvcXmlOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddXmlSerializerFormatterServices(builder.Services);
            builder.Services.Configure(setupAction);
            return builder;
        }

        // Internal for testing.
        internal static void AddXmlDataContractSerializerFormatterServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, XmlDataContractSerializerMvcOptionsSetup>());
        }

        // Internal for testing.
        internal static void AddXmlSerializerFormatterServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, XmlSerializerMvcOptionsSetup>());
        }
    }
}
