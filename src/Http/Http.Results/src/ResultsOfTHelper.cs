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

    // TODO: Improve calling static interface method with reflection
    // https://github.com/dotnet/aspnetcore/issues/46267
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
                // Method won't be found if it is from a default interface implementation.
                // Improve with https://github.com/dotnet/aspnetcore/issues/46267
                if (populateMetadataMethod is null)
                {
                    throw new InvalidOperationException($"Couldn't populate metadata for {typeof(TTarget).Name}. PopulateMetadata must by defined on the result type. A default interface implementation isn't supported with AOT.");
                }
                Debug.Assert(populateMetadataMethod != null, $"Couldn't find PopulateMetadata method on {typeof(TTarget)}.");

                populateMetadataMethod.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, parameters, culture: null);
            }
        }

        [RequiresDynamicCode("The native code for the PopulateMetadata<TTarget> instantiation might not be available at runtime.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The call to MakeGenericMethod is safe due to the fact that  PopulateMetadata<TTarget> does not have a DynamicallyAccessMembers attribute and TTarget is annotated to preserve all methods to preserve the PopulateMetadata method.")]
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
