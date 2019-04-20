// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcJsonOptionsExtensions
    {
        /// <summary>
        /// Configures the casing behavior of JSON serialization to use camel case for property names,
        /// and optionally for dynamic types and dictionary keys.
        /// </summary>
        /// <remarks>
        /// This method modifies <see cref="JsonSerializerSettings.ContractResolver"/>.
        /// </remarks>
        /// <param name="options"><see cref="MvcJsonOptions"/></param>
        /// <param name="processDictionaryKeys">If true will camel case dictionary keys and properties of dynamic objects.</param>
        /// <returns><see cref="MvcJsonOptions"/> with camel case settings.</returns>
        public static MvcJsonOptions UseCamelCasing(this MvcJsonOptions options, bool processDictionaryKeys)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.SerializerSettings.ContractResolver is DefaultContractResolver resolver)
            {
                resolver.NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = processDictionaryKeys
                };
            }
            else
            {
                if (options.SerializerSettings.ContractResolver == null)
                {
                    throw new InvalidOperationException(Resources.FormatContractResolverCannotBeNull(nameof(JsonSerializerSettings.ContractResolver)));
                }

                var contractResolverName = options.SerializerSettings.ContractResolver.GetType().Name;
                throw new InvalidOperationException(
                    Resources.FormatInvalidContractResolverForJsonCasingConfiguration(contractResolverName, nameof(DefaultContractResolver)));
            }

            return options;
        }

        /// <summary>
        /// Configures the casing behavior of JSON serialization to use the member's casing for property names,
        /// properties of dynamic types, and dictionary keys.
        /// </summary>
        /// <remarks>
        /// This method modifies <see cref="JsonSerializerSettings.ContractResolver"/>.
        /// </remarks>
        /// <param name="options"><see cref="MvcJsonOptions"/></param>
        /// <returns><see cref="MvcJsonOptions"/> with member casing settings.</returns>
        public static MvcJsonOptions UseMemberCasing(this MvcJsonOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.SerializerSettings.ContractResolver is DefaultContractResolver resolver)
            {
                resolver.NamingStrategy = new DefaultNamingStrategy();
            }
            else
            {
                if (options.SerializerSettings.ContractResolver == null)
                {
                    throw new InvalidOperationException(Resources.FormatContractResolverCannotBeNull(nameof(JsonSerializerSettings.ContractResolver)));
                }

                var contractResolverName = options.SerializerSettings.ContractResolver.GetType().Name;
                throw new InvalidOperationException(
                    Resources.FormatInvalidContractResolverForJsonCasingConfiguration(contractResolverName, nameof(DefaultContractResolver)));
            }

            return options;
        }
    }
}