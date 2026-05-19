// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for Mvc.Newtonsoft.Json options.
/// </summary>
public static class MvcNewtonsoftJsonOptionsExtensions
{
    /// <summary>
    /// Configures the casing behavior of JSON serialization to use camel case for property names,
    /// and optionally for dynamic types and dictionary keys.
    /// </summary>
    /// <remarks>
    /// This method modifies <see cref="JsonSerializerSettings.ContractResolver"/>.
    /// </remarks>
    /// <param name="options"><see cref="MvcNewtonsoftJsonOptions"/></param>
    /// <param name="processDictionaryKeys">If true will camel case dictionary keys and properties of dynamic objects.</param>
    /// <returns><see cref="MvcNewtonsoftJsonOptions"/> with camel case settings.</returns>
    public static MvcNewtonsoftJsonOptions UseCamelCasing(this MvcNewtonsoftJsonOptions options, bool processDictionaryKeys)
    {
        ArgumentNullException.ThrowIfNull(options);

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
    /// <param name="options"><see cref="MvcNewtonsoftJsonOptions"/></param>
    /// <returns><see cref="MvcNewtonsoftJsonOptions"/> with member casing settings.</returns>
    public static MvcNewtonsoftJsonOptions UseMemberCasing(this MvcNewtonsoftJsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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
