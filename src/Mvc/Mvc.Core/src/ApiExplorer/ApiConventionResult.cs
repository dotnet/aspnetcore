// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Metadata associated with an action method via API convention.
/// </summary>
public sealed class ApiConventionResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApiConventionResult"/>.
    /// </summary>
    /// <param name="responseMetadataProviders">The sequence of <see cref="IApiResponseMetadataProvider"/> that are associated with the action.</param>
    public ApiConventionResult(IReadOnlyList<IApiResponseMetadataProvider> responseMetadataProviders)
    {
        ResponseMetadataProviders = responseMetadataProviders ??
            throw new ArgumentNullException(nameof(responseMetadataProviders));
    }

    /// <summary>
    /// Gets the sequence of <see cref="IApiResponseMetadataProvider"/> that are associated with the action.
    /// </summary>
    public IReadOnlyList<IApiResponseMetadataProvider> ResponseMetadataProviders { get; }

    internal static bool TryGetApiConvention(
        MethodInfo method,
        ApiConventionTypeAttribute[] apiConventionAttributes,
        [NotNullWhen(true)] out ApiConventionResult? result)
    {
        var apiConventionMethodAttribute = method.GetCustomAttribute<ApiConventionMethodAttribute>(inherit: true);
        var conventionMethod = apiConventionMethodAttribute?.Method;
        if (conventionMethod == null)
        {
            conventionMethod = GetConventionMethod(method, apiConventionAttributes);
        }

        if (conventionMethod != null)
        {
            var metadataProviders = conventionMethod.GetCustomAttributes(inherit: false)
                .OfType<IApiResponseMetadataProvider>()
                .ToArray();

            result = new ApiConventionResult(metadataProviders);
            return true;
        }

        result = null;
        return false;
    }

    private static MethodInfo? GetConventionMethod(MethodInfo method, ApiConventionTypeAttribute[] apiConventionAttributes)
    {
        foreach (var attribute in apiConventionAttributes)
        {
            var conventionMethods = attribute.ConventionType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var conventionMethod in conventionMethods)
            {
                if (ApiConventionMatcher.IsMatch(method, conventionMethod))
                {
                    return conventionMethod;
                }
            }
        }

        return null;
    }
}
