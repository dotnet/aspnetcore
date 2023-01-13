// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

internal static class ResultsOfTHelper
{
    public const DynamicallyAccessedMemberTypes RequireMethods = DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods;
    private static readonly MethodInfo PopulateMetadataMethod = typeof(ResultsOfTHelper).GetMethod(nameof(PopulateMetadata), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static void PopulateMetadataIfTargetIsIEndpointMetadataProvider<[DynamicallyAccessedMembers(RequireMethods)] TTarget>(MethodInfo method, EndpointBuilder builder)
    {
        if (typeof(IEndpointMetadataProvider).IsAssignableFrom(typeof(TTarget)))
        {
            var parameters = new object[] { method, builder };

            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                InvokeGenericPopulateMetadata(parameters);
            }
            else
            {
                // Prioritize explicit implementation.
                var populateMetadataMethod = typeof(TTarget).GetMethod("Microsoft.AspNetCore.Http.Metadata.IEndpointMetadataProvider.PopulateMetadata", BindingFlags.Static | BindingFlags.NonPublic);
                if (populateMetadataMethod is null)
                {
                    populateMetadataMethod = typeof(TTarget).GetMethod("PopulateMetadata", BindingFlags.Static | BindingFlags.Public);
                }
                Debug.Assert(populateMetadataMethod != null, $"Couldn't find PopulateMetadata method on {typeof(TTarget)}.");

                populateMetadataMethod.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters, culture: null);
            }
        }

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Validated with IsDynamicCodeSupported check.")]
        static void InvokeGenericPopulateMetadata(object[] parameters)
        {
            PopulateMetadataMethod.MakeGenericMethod(typeof(TTarget)).Invoke(null, parameters);
        }
    }

    private static void PopulateMetadata<TTarget>(MethodInfo method, EndpointBuilder builder) where TTarget : IEndpointMetadataProvider
    {
        TTarget.PopulateMetadata(method, builder);
    }
}
